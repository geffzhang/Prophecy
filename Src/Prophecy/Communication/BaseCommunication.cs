/* **********************************************************************************
 * Copyright (c) 2011 John Hughes
 *
 * Prophecy is licenced under the Microsoft Reciprocal License (Ms-RL).
 *
 * Project Website: http://prophecy.codeplex.com/
 * **********************************************************************************/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.ComponentModel;
using Prophecy;
using Prophecy.Logger;

namespace Prophecy.Communication
{
    /// <summary>
    /// Provides data for the ConnectionAttemptFailed event.
    /// </summary>
    public class ConnectionAttemptFailedEventArgs : EventArgs
    {
        public Exception Exception;

        public ConnectionAttemptFailedEventArgs(Exception ex)
        {
            this.Exception = ex;
        }
    }

    /// <summary>
    /// Provides data for the ReceivedDelimitedString event.
    /// </summary>
    public class ReceivedDelimitedStringEventArgs : EventArgs
    {
        public string RawResponse;
        public bool MessageIncludesDelimiter;

        public ReceivedDelimitedStringEventArgs(string response, bool messageIncludesDelimiter)
        {
            RawResponse = response;
            MessageIncludesDelimiter = messageIncludesDelimiter;
        }
    }

    /// <summary>
    /// Provides data for the ReceivedBytes event.
    /// </summary>
    public class ReceivedBytesEventArgs : EventArgs
    {
        /// <summary>
        /// The incoming data buffer.
        /// </summary>
        public byte[] ReceiveBuffer;
        /// <summary>
        /// The number of bytes of data in ReceiveBuffer starting at index 0.
        /// </summary>
        public int ByteCount;

        public ReceivedBytesEventArgs(byte[] data, int byteCount)
        {
            this.ReceiveBuffer = data;
            this.ByteCount = byteCount;
        }
    }

    /// <summary>
    /// Provides data for the ReceivedString event.
    /// </summary>
    public class ReceivedStringEventArgs : EventArgs
    {
        /// <summary>
        /// The incoming data buffer decoded using the CurrentEncoding property.
        /// </summary>
        public string ReceiveBuffer;

        public ReceivedStringEventArgs(string data)
        {
            this.ReceiveBuffer = data;
        }
    }


    internal class BaseCommunicationSendQueueItem
    {
        public byte[] Data;
        public int DelayMilliseconds;

        public BaseCommunicationSendQueueItem(byte[] data, int delayMilliseconds)
        {
            Data = data;
            DelayMilliseconds = delayMilliseconds;
        }
    }

    public abstract class BaseCommunication : IDisposable, ICommunication
    {
        enum ConnectionMonitorState
        {
            AttemptingToConnect,
            Connected,
            Connected_WaitingForResponse
        }

        private ConnectionMonitorState _connectionMonitorState;

        private Encoding _encoding = System.Text.Encoding.ASCII;
        private string _delimiter = "\r\n";
        private bool _includeDelimiterInRawResponse = false;

        private string _readBufferString = ""; // This is the buffer of received string data.

        private byte[] _stringDecodingBuffer = new byte[0];
        //private int _readBufferByteCount = 0;

        /// <summary>
        /// Occurs when a delimited string is received. A delimited string is detected by using the value in the Delimiter property. If the Delimiter property is null this event will not occur.  The EventArgs contain the delimited string. The IncludeDelimiterInRawResponse property indicates if the delimited string will include the delimiter.
        /// </summary>
        public event EventHandler<ReceivedDelimitedStringEventArgs> ReceivedDelimitedString;
        /// <summary>
        /// Occurs when data is received. The received bytes will be in the EventArgs.  If the ReadBufferEnabled property is true, you may also process the ReadBuffer property.
        /// </summary>
        public event EventHandler<ReceivedBytesEventArgs> ReceivedBytes;
        /// <summary>
        /// Occurs when string data is received. The received string will be in the EventArgs.
        /// </summary>
        public event EventHandler<ReceivedStringEventArgs> ReceivedString;

        private bool _readBufferEnabled = false;
        private BufferReader _bufferReader;
        
        /// <summary>
        /// The ReadBufferOverflow event will occur when the buffer size limit has been exceeded. The overflowed data will be lost.
        /// </summary>
        public int MaximumReadBufferSize = 4096;
        
        /// <summary>
        /// Occrus when the ReadBufferEnabled property is true and the buffer size limit has been exceeded. The overflowed data will be lost.
        /// </summary>
        public event EventHandler ReadBufferOverflow;

        /// <summary>
        /// Occurs after no data has been received in the specified amount of time and the object's connection monitor would like to send a heartbeat to ensure the connection is still established.
        /// This is usually used as an alternative to ConnectionMonitorTestRequest or ConnectionMonitorTestRequestBytes when a more complicated heartbeat/request is necessary.
        /// </summary>
        public event EventHandler ConnectionMonitorTest;
        
        /// <summary>
        /// Occurs when the connection is reestablished.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionEstablished;

        /// <summary>
        /// Occurs when a connection was attempted but failed.
        /// </summary>
        public event EventHandler<ConnectionAttemptFailedEventArgs> ConnectionAttemptFailed;

        /// <summary>
        /// Occurs when the connection is lost.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionLost;

        System.Threading.Timer _connectionMonitorTimer; // this is only use when connection monitoring is enabled.
        private DateTime _lastReceivedDataDateTime = DateTime.MinValue;
        private bool _isConnected = false; // indicates if data has been received within the the last X seconds

        private string _connectionMonitorTestRequestString = null;
        private byte[] _connectionMonitorTestRequestBytes = null;
        private bool _connectionMonitorEnabled = false;
        private int _connectionMonitorTimeout = 30000; // the amount of time to wait for any data to be received before we send _connectionMonitorTestRequestBytes.
        private int _connectionMonitorWaitForResponseTimeout = 30000; // the amount of time to wait after we send the _connectionMonitorTestRequestBytes.
        private int _connectionAttemptInterval = 5000; // the interval to use between connection attempts.

        private Queue<BaseCommunicationSendQueueItem> _sendQueue = new Queue<BaseCommunicationSendQueueItem>();
        private AutoResetEvent _sendQueueEvent = new AutoResetEvent(false);
        private int _defaultSendDelayInterval = 0;

        ulong _totalReceivedByteCount = 0;
        ulong _totalSendByteCount = 0;

        System.Threading.Timer _isCommunicatingTimer; // keeps track of when we are sending and/or receiving.
        public event EventHandler CommunicationStarted;
        public event EventHandler CommunicationEnded;

        public abstract string ConnectionDisplayText { get; }

        protected ILogger _logger = new StubLogger();

        private bool _disposed = false;

        public BaseCommunication()
        {
            // Start a new thread that waits for items to be added to the send queue.
            Thread t = new Thread(new ThreadStart(sendQueueWorker));
            t.IsBackground = true; // Do not prevent the process from terminating if the user forgets to dispose this class.
            t.Start();

            //_bufferReader = new BufferReader(_encoding); default to disabled.... the user must set the ReadBufferEnabled property.
        }

        void sendQueueWorker()
        {
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                while (_disposed == false)
                {
                    try
                    {
                        // Wait for an item to be added to the queue or to be canceled.
                        _sendQueueEvent.WaitOne();

                        // Check if canceled while waiting.
                        if (_disposed)
                            return; // Gracefully end the thread.


                        while (_disposed == false && _sendQueue.Count > 0)
                        {
                            try
                            {
                                // Get the queued item.
                                BaseCommunicationSendQueueItem item = _sendQueue.Dequeue();
                                if (item == null || item.Data == null) // just in case a null creeps into the queue but I don't think this can ever happen.
                                    continue;

                                // Make sure enough time has passed since the previous send.
                                while (sw.ElapsedMilliseconds < item.DelayMilliseconds)
                                {
                                    // Check if canceled while processing.
                                    if (_disposed)
                                        return; // Gracefully end the thread.

                                    // Sleep for a very brief moment.
                                    System.Threading.Thread.Sleep(5); // TODO: ideally this would use a manualresetevent with a timeout set
                                    //System.Diagnostics.Debug.WriteLine("WAITING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                }

                                startCommunicatingTimer();

                                // Send the data.
                                bool successfulSend = true;
                                try
                                {
                                    sendData(item.Data);
                                }
                                catch (ObjectDisposedException ex) // sendData() throws a ObjectDisposedException is the connection is lost.
                                {
                                    successfulSend = false;
                                }

                                try
                                {
                                    if (successfulSend)
                                    {
                                        if (_totalSendByteCount != ulong.MaxValue) // did we already max out?
                                            checked // force OverflowException on overflow.
                                            {
                                                _totalSendByteCount += (ulong)item.Data.Length;
                                            }
                                    }
                                }
                                catch (OverflowException)
                                {
                                    _totalSendByteCount = ulong.MaxValue;
                                }

                                // Restart the stopwatch.
                                sw.Reset(); // stops the stopwatch and resets it to zero.
                                sw.Start(); // start the stopwatch.
                            }
                            catch (Exception ex)
                            {
                                if (_disposed)
                                    return; // Gracefully end the thread.
                                else
                                    _logger.Error("An error occurred in the send queue worker thread (3).", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_disposed)
                            return; // Gracefully end the thread.
                        else
                            _logger.Error("An error occurred in the send queue worker thread (2).", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_disposed)
                    return; // Gracefully end the thread.
                else
                    _logger.Error("An error occurred in the send queue worker thread (1).", ex);
            }
        }

        /// <summary>
        /// Gets a value indicating if the ReadBuffer is enabled.
        /// </summary>
        public bool ReadBufferEnabled
        {
            get { return _readBufferEnabled; }
            set
            {
                _readBufferEnabled= value; // this must be first.

                if (value)
                {
                    if (_bufferReader == null)
                    {
                        _bufferReader = new BufferReader(_encoding);
                    }
                }
                else
                {
                    if (_bufferReader != null)
                    {
                        _bufferReader.Dispose();
                        _bufferReader = null;
                    }
                }

            }
        }
        public BufferReader ReadBuffer
        {
            get
            {
                if (_readBufferEnabled == false)
                    throw new InvalidOperationException("The ReadBufferEnabled property is false so the ReadBuffer property is not available. Set the ReadBufferEnabled property to true to enable the buffer.");

                return _bufferReader;
            }
        }

        /// <summary>
        /// Gets a value indicating if connection monitoring enabled.
        /// </summary>
        public virtual bool ConnectionMonitorEnabled
        {
            get { return _connectionMonitorEnabled; }
        }

        /// <summary>
        /// The ILogger object to use when logging. If this is not specified then the system logger will be used.
        /// </summary>
        public virtual ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Attempts to open a connection if there is currently no connection open, and starts the connection monitor.  The ConnectionEstablished event occurs when the connection is established, and the ConnectionAttemptFailed event occurs when a connection attempt fails.
        /// No Exceptions are thrown.
        /// This method is run asynchronously and returns immediately.
        /// </summary>
        public virtual void StartConnectionMonitor()
        {
            Thread t = new Thread(new ThreadStart(StartConnectionMonitorSync));
            t.IsBackground = true; // Do not prevent the process from terminating if the user forgets to dispose this class.
            t.Start();
        }

        /// <summary>
        /// Attempts to open a connection if there is currently no connection open, and starts the connection monitor.  The ConnectionEstablished event occurs when the connection is established, and the ConnectionAttemptFailed event occurs when a connection attempt fails.
        /// No Exceptions are thrown.
        /// The first connection attempt blocks the thread so the Connected property should be checked after invoking StartConnectionMonitor() to determine if the inital connection attempt failed.
        /// </summary>
        public virtual void StartConnectionMonitorSync()
        {
            try
            {
                if (_isConnected == false)
                    OnConnectionAttempt();  // this will call the highest level overridden method, so if it is overridden then the override will be called instead of the method in this class.
            }
            catch
            {
                // don't throw an error if it fails since we start the connection monitor below.
            }

			
			// Check if OnConnectionAttemptFailed() or OnConnectionEstablished() disposed the conneciton.
            // This is typical when first starting up and the connection fails, the app might dispose the object so the user can change the name.
            if (_disposed)
                return; // don't start up the connection monitoring since the object was disposed.


//System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "************************************** StartConnectionMonitor Setting timer interval to " + _connectionAttemptInterval);
            if (_connectionMonitorTimer == null)
            {
//System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "************************************** _connectionMonitorTimer is null, creating new timer.");
                // Set up dropped connection check timer.
                System.Threading.TimerCallback timerDelegate = new System.Threading.TimerCallback(_connectionMonitorTimer_Elapsed);
                _connectionMonitorTimer = new System.Threading.Timer(timerDelegate, null, _connectionAttemptInterval, System.Threading.Timeout.Infinite);
            }
            else
                _connectionMonitorTimer.Change(_connectionAttemptInterval, System.Threading.Timeout.Infinite);
//System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "************************************** _connectionMonitorTimer has been set.");
            _connectionMonitorEnabled = true;
        }


        /// <summary>
        /// Stops the connection monitoring.
        /// </summary>
        public virtual void StopConnectionMonitor()
        {
            _connectionMonitorEnabled = false;

            if (_connectionMonitorTimer != null)
            {
                _connectionMonitorTimer.Dispose();
                _connectionMonitorTimer = null;
            }
        }
        
        /// <summary>
        /// The number of milliseconds of no data received before the ConnectionMonitorTestRequest is sent to request a response.
        /// </summary>
        public virtual int ConnectionMonitorTimeout
        {
            get { return _connectionMonitorTimeout; }
            set { _connectionMonitorTimeout = value; }
        }

        /// <summary>
        /// The full data string (including any end of line characters) that is sent to request a response if no data has been received in the number of milliseconds specified in ConnectionMonitorTimeout.
        /// This will set ConnectionMonitorTestBytes using the CurrentEncoding.
        /// </summary>
        public virtual string ConnectionMonitorTestRequest
        {
            get { return _connectionMonitorTestRequestString; }
            set
            {
                _connectionMonitorTestRequestString = value;
                if (value == null)
                    _connectionMonitorTestRequestBytes = null;
                else
                    _connectionMonitorTestRequestBytes = CurrentEncoding.GetBytes(value);
            }
        }

        /// <summary>
        /// The full byte array data (including any end of line characters) that is sent to request a response if no data has been received in the number of milliseconds specified in ConnectionMonitorTimeout.
        /// </summary>
        public virtual byte[] ConnectionMonitorTestBytes
        {
            get { return _connectionMonitorTestRequestBytes; }
            set
            {
                _connectionMonitorTestRequestString = null;
                _connectionMonitorTestRequestBytes = value;
            }
        }

        void _connectionMonitorTimer_Elapsed(Object stateInfo)
        {
            if (_connectionMonitorEnabled)
            {
                int timerInterval = _connectionMonitorTimeout;

                // If we haven't received any data in a while...
                try
                {
                    TimeSpan timeSinceReceived = DateTime.Now - _lastReceivedDataDateTime;
                    //System.Diagnostics.Debug.WriteLine("TIME SINCE LAST RECEIVED DATA: " + timeSinceReceived.ToString());


                    switch (_connectionMonitorState)
                    {
                        case ConnectionMonitorState.AttemptingToConnect:
                            // Attempt to make a connection
                            try
                            {
                                OnConnectionAttempt(); // the serial class overrides this and does nothing if the serial connection has already been opened.
                            }
                            catch
                            {
                                // don't throw an error if it fails
                            }

                            timerInterval = _connectionAttemptInterval;

                            break;

                        case ConnectionMonitorState.Connected:
                            // We haven't received data in the time limit so send out the test connection request.

                            _connectionMonitorState = ConnectionMonitorState.Connected_WaitingForResponse; // set this to waiting whether there is a test request to send out or not.

                            if (_connectionMonitorTestRequestBytes != null && _connectionMonitorTestRequestBytes.Length > 0)
                            {
                                // Send the request since we haven't received anything for a while.
                                try
                                {
                                    timerInterval = _connectionMonitorWaitForResponseTimeout;

                                    //#if DEBUG
                                    //if (_connectionMonitorTestRequestString != null && _connectionMonitorTestRequestString.Length > 0)
                                    //    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  _connectionMonitorTimer_Elapsed() sending: " + _connectionMonitorTestRequestString.Replace("\r", "<CR>").Replace("\n", "<LF>"));
                                    //else
                                    //    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  _connectionMonitorTimer_Elapsed() sending: " + global::Common.SystemUtility.ByteArrayToHexString(_connectionMonitorTestRequestBytes, " "));
                                    //#endif

                                    Send(_connectionMonitorTestRequestBytes);
                                }
                                catch
                                {
                                    // just catch the error in and continue
                                }
                            }

                            // Fire the ConnectionMonitorTest event.
                            OnConnectionMonitorTest();
                            break;

                        case ConnectionMonitorState.Connected_WaitingForResponse:
                            // We were connected, but didn't receive a response after the request,
                            // so we are now considered disconnected.
                            _logger.Error("Connection monitoring sent a request after " + (_connectionMonitorTimeout/1000) + " seconds of device silence and failed to receive a response from the device within " + (_connectionMonitorWaitForResponseTimeout/1000) + " seconds so the connection is assumed to be lost.");
                            OnConnectionLost();

                            timerInterval = _connectionAttemptInterval;

                            // Attempt to reconnect
                            try
                            { 
                                OnConnectionAttempt(); // the serial class overrides this and does nothing if the serial connection has already been opened.
                            }
                            catch
                            {
                                // don't throw an error if it fails
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    // do nothing
                }
                finally
                {
                    if (_connectionMonitorEnabled && _connectionMonitorTimer != null) // just in case it was disabled while the above was running.
                    {
                        //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "************************************** _connectionMonitorTimer_Elapsed-finally Setting timer interval to " + timerInterval);

                        try
                        {
                            // I've seen this be null even though we just checked that, so I wrapped it in a try/catch.
                            _connectionMonitorTimer.Change(timerInterval, System.Threading.Timeout.Infinite); 
                        }
                        catch
                        {
                            // _connectionMonitorTimer was probably null so ignore it.
                        }
                    }
                }
            }
        }

        private readonly object _readBufferString_lock = new object();
        /// <summary>
        /// Add the specified data to the internal buffer and parses the buffer for delimited data.
        /// </summary>
        public void ProcessReceivedData(byte[] data, int byteCount)
        {
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  ProcessReceivedData received utf8:   " + Encoding.UTF8.GetString(data, 0, byteCount));

            lock (_readBufferString_lock)
            {
                _lastReceivedDataDateTime = DateTime.Now;

                startCommunicatingTimer();

                try
                {
                    if (_totalReceivedByteCount != ulong.MaxValue) // did we already max out?
                        checked // force OverflowException on overflow.
                        {
                            _totalReceivedByteCount += (ulong)byteCount;
                        }
                }
                catch (OverflowException)
                {
                    _totalReceivedByteCount = ulong.MaxValue;
                }

                if (_connectionMonitorEnabled)
                {
                    if (_connectionMonitorTimer != null)
                        _connectionMonitorTimer.Change(_connectionMonitorTimeout, System.Threading.Timeout.Infinite);

                    _connectionMonitorState = ConnectionMonitorState.Connected;
                }

                //**************************************************************
                // Check for connection reestablished
                //**************************************************************
                if (_isConnected == false)
                    OnConnectionEstablished();


                //**************************************************************
                // Write the data to the user accessible buffer.
                //**************************************************************
                if (_bufferReader != null && MaximumReadBufferSize > 0)
                {
                    if (_bufferReader.Length + byteCount <= MaximumReadBufferSize)
                    {
                        // Append all the incoming bytes to the buffer.
                        _bufferReader.WriteInternal(data, 0, byteCount);
                    }
                    else
                    {
                        // Fill the rest of the buffer with as many bytes as we can.
                        _bufferReader.WriteInternal(data, 0, MaximumReadBufferSize - (int)_bufferReader.Length);
                        OnBufferOverflow(); // let the user know that the buffer overflowed.
                    }
                }


                //*****************************************************************************************************************
                // Fire the ReceivedBytes.
                //*****************************************************************************************************************
                OnReceivedBytes(data, byteCount);


                //*******************************************************
                // Decode the bytes to a string.
                //*******************************************************
                string receivedString = null;
                if (_encoding != null)
                {
                    //*******************************************************
                    // Copy the data to the end of the existing byte buffer.
                    //*******************************************************
                    try
                    {
                        byte[] tempStringDecodingBuffer = new byte[_stringDecodingBuffer.Length + byteCount];
                        System.Buffer.BlockCopy(_stringDecodingBuffer, 0, tempStringDecodingBuffer, 0, _stringDecodingBuffer.Length);
                        System.Buffer.BlockCopy(data, 0, tempStringDecodingBuffer, _stringDecodingBuffer.Length, byteCount);
                        _stringDecodingBuffer = tempStringDecodingBuffer;
                        tempStringDecodingBuffer = null;
                    }
                    catch (Exception ex)
                    {
                        //System.Diagnostics.Debugger.Break();
                        _logger.Error("An error occurred while copying the data to the end of the existing byte buffer.", ex);
                    }

                    //*********************************************************************************************************************
                    // Convert the bytes to a string (keeping track of how any good bytes there are in case a character spans two buffers.
                    //*********************************************************************************************************************
                    Decoder decoder = _encoding.GetDecoder();
                    int charCount = decoder.GetCharCount(_stringDecodingBuffer, 0, _stringDecodingBuffer.Length);
                    char[] chars = new char[charCount];
                    decoder.GetChars(_stringDecodingBuffer, 0, _stringDecodingBuffer.Length, chars, 0);


                    //**************************************************************
                    // Add the received string to the buffer.
                    //**************************************************************
                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  ProcessReceivedData buffer before: " + _readBufferString);
                    receivedString = new string(chars);
                    _readBufferString += receivedString;
                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  ProcessReceivedData buffer after:  " + _readBufferString);


                    // Determine how many bytes were actually decoded.
                    Encoder encoder = _encoding.GetEncoder();
                    int decodedByteCount = encoder.GetByteCount(chars, 0, charCount, false);

                    // Determine how many bytes at the end of the buffer are unprocessed partial utf8 bytes.
                    int unprocessedByteCount = _stringDecodingBuffer.Length - decodedByteCount;

                    try
                    {
                        // Set _stringDecodingBuffer to the unprocessed left over (partial character) bytes at the end.
                        byte[] tempStringDecodingBuffer = new byte[unprocessedByteCount];
                        System.Buffer.BlockCopy(_stringDecodingBuffer, decodedByteCount, tempStringDecodingBuffer, 0, unprocessedByteCount);
                        _stringDecodingBuffer = tempStringDecodingBuffer;
                        tempStringDecodingBuffer = null;
                    }
                    catch (Exception ex)
                    {
                        //System.Diagnostics.Debugger.Break();
                        _logger.Error("An error occurred while setting _stringDecodingBuffer to the unprocessed left over (partial character) bytes at the end.", ex);
                    }
                }

                //*****************************************************************************************************************
                // Fire the ReceivedString.
                //*****************************************************************************************************************
                if (string.IsNullOrEmpty(receivedString)) // only fire the event if we decoded at least one character.
                    OnReceivedString(receivedString);


                // NOTE: I WAS USING a StringBuilder object for both the logs and the input buffer, but it unexpectidly seemed to causes crashes!?!?

                if (_delimiter != null && _readBufferString.Length > 0)
                {
                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  " + "BaseCommunication.ProcessReceivedData() processing: " + _readBufferString.Replace("\r", "<CR>").Replace("\n", "<LF>"));


                    // Loop over buffer processing any full responses.
                    int eolPos = _readBufferString.IndexOf(_delimiter);
                    while (eolPos >= 0)
                    {
                        // Pull out response from buffer
                        string response;
                        if (_includeDelimiterInRawResponse)
                            response = _readBufferString.Substring(0, eolPos + _delimiter.Length).Trim();
                        else
                            response = _readBufferString.Substring(0, eolPos).Trim();
                        _readBufferString = _readBufferString.Remove(0, eolPos + _delimiter.Length);


                        //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  ProcessReceivedData extracted response:  " + response);
                        //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  ProcessReceivedData buffer after removal:  " + _readBufferString);

                        //TODO:OnLog("port_DataReceived() received '" + response + "'");

                        OnReceivedDelimitedString(response, _includeDelimiterInRawResponse);

                        eolPos = _readBufferString.IndexOf(_delimiter);
                    }
                }
                else
                {
                    // There is no delimiter, so just clear the string buffer so it doesn't grow.
                    _readBufferString = "";
                    // Don't clear the byte buffer since it can contain partial character bytes for the decoder.
                }
            

                //TODO:OnLog("port_DataReceived() Exiting ^-^-^-^-^");
            }
        }


        
        /// <summary>
        /// Gets or sets the end of response delimiter. Set to null to not parse using a delimiter.
        /// </summary>
        public virtual string Delimiter
        {
            get { return _delimiter; }
            set { _delimiter = value; }
        }

        public virtual bool IncludeDelimiterInRawResponse
        {
            get { return _includeDelimiterInRawResponse; }
            set { _includeDelimiterInRawResponse = value; }
        }

        /// <summary>
        /// Gets or sets the current character encoding that the serial port object using. Set to null to not decode to a string.
        /// </summary>
        public virtual Encoding CurrentEncoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;

                if (_bufferReader != null)
                    _bufferReader.CurrentEncoding = value;
            }
        }

        /// <summary>
        /// The default number of milliseconds to ensure have passed between the sending data packets. The Send methods can also optionally accept a delay value.
        /// </summary>
        public int DefaultSendDelayInterval
        {
            get { return _defaultSendDelayInterval; }
            set { _defaultSendDelayInterval = value; }
        }

        protected virtual void OnReceivedDelimitedString(string message, bool messageIncludesDelimiter)
        {
            if (ReceivedDelimitedString != null)
            {
                try
                {
                    ReceivedDelimitedString(this, new ReceivedDelimitedStringEventArgs(message, messageIncludesDelimiter));
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        protected virtual void OnReceivedBytes(byte[] data, int byteCount)
        {
            if (ReceivedBytes != null)
            {
                try
                {
                    ReceivedBytes(this, new ReceivedBytesEventArgs(data, byteCount));
                }
                catch (Exception ex)
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        protected virtual void OnReceivedString(string s)
        {
            if (ReceivedString != null)
            {
                try
                {
                    ReceivedString(this, new ReceivedStringEventArgs(s));
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        protected virtual void OnBufferOverflow()
        {
            if (ReadBufferOverflow != null)
            {
                try
                {
                    ReadBufferOverflow(this, new EventArgs());
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }
        

        protected virtual void OnConnectionEstablished()
        {
            _isConnected = true;

            if (ConnectionEstablished != null)
            {
                try
                {
                    ConnectionEstablished(this, new EventArgs());
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        protected virtual void OnConnectionAttemptFailed(Exception ex)
        {
            if (ConnectionAttemptFailed != null)
            {
                try
                {
                    ConnectionAttemptFailed(this, new ConnectionAttemptFailedEventArgs(ex));
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        protected virtual void OnConnectionLost()
        {
            _isConnected = false;
            _connectionMonitorState = ConnectionMonitorState.AttemptingToConnect;

            if (ConnectionLost != null)
            {
                try
                {
                    ConnectionLost(this, new EventArgs());
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }

            try
            {
                // Since OnConnectionLost can be called from a superclass (such as from the TcpCommunication.readCallback()) we need to change the timer to _connectionAttemptInterval.
                if (_connectionMonitorEnabled && _connectionMonitorTimer != null) // just in case it was disabled while the above was running.
                {
                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "************************************** OnConnectionLost Setting timer interval to " + _connectionAttemptInterval);

                    _connectionMonitorTimer.Change(_connectionAttemptInterval, System.Threading.Timeout.Infinite);
                }
            }
            catch
            {
            }
        }

        protected virtual void OnConnectionAttempt()
        {
            try
            {
                open(); // attempt to open the connection.
            }
            catch (Exception ex)
            {
                // If it failed to connect the just return. The driver should test the Connected property after calling Open() or StartMonitoring().

                // The typical reasons for failure at this time are:
                // * Serial Port does not exist: NOT RECOVERABLE, however the configuration system only shows existing COM ports when the user created the device which is helpful.
                // * Serial Port already opened: RECOVERABLE (unless more than one driver is configured to use the same port)... once the other process releases the serial port, the connection monitoring will have access to the port.
                // * Tcp connection could not be established: RECOVERABLE if the Address is correct.
                // * Tcp network card is not installed: NOT RECOVERABLE

                OnConnectionAttemptFailed(ex);

                throw;
            }

            // Successfull Connection
            _connectionMonitorState = ConnectionMonitorState.Connected;
            OnConnectionEstablished();
        }

        protected virtual void OnConnectionMonitorTest()
        {
            if (ConnectionMonitorTest != null)
            {
                try
                {
                    ConnectionMonitorTest(this, new EventArgs());
                }
                catch
                {
                    // just catch the error in the user code and continue.
                }
            }
        }

        /// <summary>
        /// Opens the connection.  This should throw an exception if it failed to open.
        /// </summary>
        protected abstract void open();

        /// <summary>
        /// This is the method that does the actual sending (as opposed to queueing).
        /// </summary>
        /// <param name="raw"></param>
        protected abstract void sendData(byte[] raw); // THIS ACTUALLY SENDS THE DATA TO THE PORT.

        /// <summary>
        /// Opens a new connection. An exception is thrown if it fails. ConnectionEstablished or ConnectionAttemptFailed will also be raised.
        /// </summary>
        public virtual void Open()
        {
            OnConnectionAttempt();
        }

        /// <summary>
        /// Enqueue a string to be sent as soon as all previously queued items have been sent.
        /// </summary>
        /// <param name="s">The string to enqueue to be sent.</param>
        public virtual void Send(string s)
        {
            byte[] b = CurrentEncoding.GetBytes(s);

            Send(b);
        }
        /// <summary>
        /// Enqueue a string to be sent the specified milliseconds after the previously queued item has been sent.
        /// </summary>
        /// <param name="s">The string to enqueue to be sent.</param>
        /// <param name="delayMilliseconds">The number of milliseconds to wait to send the string after the prior send has occurred.</param>
        public virtual void Send(string s, int delayMilliseconds)
        {
            byte[] b = CurrentEncoding.GetBytes(s);

            Send(b, delayMilliseconds);
        }
        /// <summary>
        /// Enqueue a byte array to be sent as soon as all previously queued items have been sent.
        /// </summary>
        /// <param name="data">The byte array to enqueue to send.</param>
        public virtual void Send(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            EnqueueDataToSend(data);
        }
        /// <summary>
        /// Enqueue a byte array to be sent the specified milliseconds after the previously queued item has been sent.
        /// </summary>
        /// <param name="data">The byte array to enqueue to send</param>
        /// <param name="delayMilliseconds">The number of milliseconds to wait to send the byte array after the prior send has occurred.</param>
        public virtual void Send(byte[] data, int delayMilliseconds)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            EnqueueDataToSend(data, delayMilliseconds);
        }

        /// <summary>
        /// Simulate data being received from the connection. The data will be processed as if it were actually received as incoming data from the tcp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public abstract void SimulateReceivedData(string data);
        /// <summary>
        /// Simulate data being received from the connection. The data will be processed as if it were actually received as incoming data from the tcp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public abstract void SimulateReceivedData(byte[] data);

        /// <summary>
        /// Add the data to queue for sending.
        /// </summary>
        /// <param name="data">The data to send.</param>
        protected void EnqueueDataToSend(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            EnqueueDataToSend(data, _defaultSendDelayInterval);
        }
        /// <summary>
        /// Add the data to queue for sending.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="delayMilliseconds">The number of milliseconds to ensure have passed between the previously sent data packet and this data packet.</param>
        protected void EnqueueDataToSend(byte[] data, int delayMilliseconds)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (_disposed == false)
            {
                _sendQueue.Enqueue(new BaseCommunicationSendQueueItem(data, delayMilliseconds));
                _sendQueueEvent.Set(); // notify the sendQueueWorker thread that an item has been added to the queue
            }
        }

        /// <summary>
        /// Indicates if the pc is successfully communicating with the the device.
        /// 
        /// For serial connections when not using connection monitoring, this will always be true as long as the serial port was successfully opened.
        /// For serial connections when using connection monitoring, this will only be true if the serial port was successfully opened and the device consistantly sends data to the pc within a specified time limit.
        /// 
        /// For tcp connections when not using connection monitoring, this will only be true as long a tcp connection was established and sending data does not cause an error.
        /// For tcp connections when using connection monitoring, this will only be true if a tcp connection was established, sending data does not cause an error, and the device consistantly sends data to the pc within a specified time limit.
        /// </summary>
        public virtual bool Connected
        {
            get
            {
                return _isConnected;
            }
        }


        public void Flush()
        {
            Flush(-1); // -1 means wait until fully flushed.
        }

        /// <summary>
        /// Waits for the send queue to send all pending packets.
        /// </summary>
        /// <param name="timeoutMilliseconds">The maximum number of milliseconds to wait for the send queue to finish before throwing a TimeoutException. -1 = infinity.</param>
        public void Flush(int timeoutMilliseconds)
        {
            if (_disposed)
                return;

            if (timeoutMilliseconds < -1)
                throw new ArgumentException("timeoutMilliseconds must be positive number, 0 or -1.");
            
            if (timeoutMilliseconds == 0)
                return;

            if (_sendQueue.Count == 0)
                return;

            try
            {
                System.Diagnostics.Stopwatch sw = null;
                if (timeoutMilliseconds > 0)
                {
                    sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                }

                // Looping with a Sleep like this is not the best way to wait for the queue to finish, but the timeout should be short and this is a simple solution.
                while (_sendQueue.Count > 0)
                {
                    if (sw != null && sw.ElapsedMilliseconds > timeoutMilliseconds)
                        throw new TimeoutException("The send queue did not complete sending within the timeout period.");

                    System.Threading.Thread.Sleep(250);
                }
            }
            catch
            {
                if (_disposed)
                    return;
                else
                    throw;
            }
        }

        /// <summary>
        /// Waits for the send queue to send all pending data and then releases all resources used by the object.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(-1);
        }

        /// <summary>
        /// Waits a maximum of the specified number of milliseconds for the send queue to send all pending data and releases all resources used by the object.
        /// </summary>
        /// <param name="timeoutMilliseconds">The maximum number of milliseconds to wait for the send queue to finish before disposing. -1 = infinity.</param>
        public virtual void Dispose(int timeoutMilliseconds)
        {
            try
            {
                Flush(timeoutMilliseconds); // Ensure everything has been sent. This is consistant with other .NET objects like FileStream and StreamWriter which auto-flush before disposing.
            }
            catch (TimeoutException)
            {
                // do nothing.
            }

            // Causes the sendQueueWorker thread to exit gracefully. Although it is run on a background thread so it will be terminated no matter what if the user forgets to dispose.
            _disposed = true;

            if (_sendQueueEvent != null)
            {
                _sendQueueEvent.Set();
                _sendQueueEvent.Close(); // you must call Set before calling Close since Close doesn't automatically do a Set.
                _sendQueueEvent = null;
            }
            
            _sendQueue.Clear();

            if (_isCommunicatingTimer != null)
            {
                _isCommunicatingTimer.Dispose();
                _isCommunicatingTimer = null;
            }

            if (_connectionMonitorTimer != null)
            {
                _connectionMonitorTimer.Dispose();
                _connectionMonitorTimer = null;
            }

            if (_bufferReader != null)
            {
                _readBufferEnabled = false;
                _bufferReader.Dispose();
                _bufferReader = null;
            }
        }


        protected byte[] HexStringToByteArray(string hexData)
        {
            try
            {
                hexData = hexData.Replace(" ", ""); // remove any spaces
                byte[] buffer = new byte[hexData.Length / 2];
                for (int i = 0; i < hexData.Length; i += 2)
                    buffer[i / 2] = (byte)Convert.ToByte(hexData.Substring(i, 2), 16);
                return buffer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("The hexData string must contain contiguous 2 character hexidecimal values.", ex);
            }
        }

        protected string ByteArrayToHexString(byte[] b)
        {
            return ByteArrayToHexString(b, 0, b.Length, "");
        }
        protected string ByteArrayToHexString(byte[] b, string delimiter)
        {
            return ByteArrayToHexString(b, 0, b.Length, delimiter);
        }
        protected string ByteArrayToHexString(byte[] b, int startIndex, int length, string delimiter)
        {
            return ByteArrayToHexString(b, startIndex, length, delimiter, int.MaxValue);
        }
        protected string ByteArrayToHexString(byte[] b, int startIndex, int length, string delimiter, int maxResultLength)
        {
            StringBuilder sb = new StringBuilder((length - startIndex) * (2 + delimiter.Length));
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (sb.Length + 3 >= maxResultLength)
                {
                    sb.Append("...");
                    break;
                }

                sb.Append(BitConverter.ToString(b, i, 1) + delimiter);
            }
            return sb.ToString();
        }



        private readonly object _isCommunicatingTimer_lock = new object();
        private void startCommunicatingTimer()
        {
            const int timeout = 500; // 250ms is too short. 500ms works well.

            lock (_isCommunicatingTimer_lock)
            {
                if (_isCommunicatingTimer == null)
                {
                    // Set up timer.
                    System.Threading.TimerCallback timerDelegate = new System.Threading.TimerCallback(_isCommunicatingTimer_Elapsed);
                    _isCommunicatingTimer = new System.Threading.Timer(timerDelegate, null, timeout, System.Threading.Timeout.Infinite);

                    OnCommunicationStarted();
                }
                else // extend timer
                    _isCommunicatingTimer.Change(timeout, System.Threading.Timeout.Infinite);
            }
        }

        void _isCommunicatingTimer_Elapsed(Object stateInfo)
        {
            // When the timer elapses, turn it off.
            lock (_isCommunicatingTimer_lock)
            {
                if (_isCommunicatingTimer != null)
                {
                    _isCommunicatingTimer.Dispose();
                    _isCommunicatingTimer = null;
                }

                OnCommunicationEnded();
            }
        }

        protected virtual void OnCommunicationStarted()
        {
            try
            {
                if (CommunicationStarted != null)
                    CommunicationStarted(this, new EventArgs());
            }
            catch
            {
            }
        }
        protected virtual void OnCommunicationEnded()
        {
            try
            {
                if (CommunicationEnded != null)
                    CommunicationEnded(this, new EventArgs());
            }
            catch
            {
            }
        }

        public bool IsCommunicating
        {
            get
            {
                //lock (_isCommunicatingTimer_lock)  DO NOT LOCK!!! since it can cause a deadlock when accessing this property from the CommunicationStarted and CommunicationEnded events.
                //{
                    return _isCommunicatingTimer != null;
                //}
            }
        }

        /// <summary>
        /// The total number of bytes that have been received.
        /// </summary>
        public ulong TotalReceivedByteCount
        {
            get { return _totalReceivedByteCount; }
        }
        /// <summary>
        /// The total number of bytes that have been sent.
        /// </summary>
        public ulong TotalSentByteCount
        {
            get { return _totalSendByteCount; }
        }
        /// <summary>
        /// Resets the total sent and received byte counts to zero.
        /// </summary>
        public void ResetByteCountTotals()
        {
            _totalReceivedByteCount = 0;
            _totalSendByteCount = 0;
        }
    }




    /// <summary>
    /// Provides access to buffered incoming data.
    /// </summary>
    public class BufferReader : Stream
    {
        // TODO: this class is not thread safe!

        MemoryStream _ms;

        Encoding _encoding;

        internal BufferReader(Encoding encoding)
        {
            _encoding = encoding;

            _ms = new MemoryStream();
        }

        /// <summary>
        /// Gets the byte at the specified index in the buffer. The byte will not be removed from the buffer.
        /// </summary>
        /// <param name="index">The index of the byte to retrieve.</param>
        /// <returns>A byte.</returns>
        public byte this[int index]
        {
            get
            {
                return _ms.GetBuffer()[index];
            }
        }

        /// <summary>
        /// Gets the number of bytes in the buffer.
        /// </summary>
        public override long Length
        {
            get { return _ms.Length; }
        }

        /// <summary>
        ///  Clear the buffer.
        /// </summary>
        public void Clear()
        {
            _ms.Position = 0;

            removeBytes((int)_ms.Length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > (int)buffer.Length)
                throw new ArgumentOutOfRangeException("offset");

            if (count < 0 || count > ((int)buffer.Length - offset))
                throw new ArgumentOutOfRangeException("size");

            if (count > _ms.Length)
                throw new ArgumentOutOfRangeException("there are not enough bytes in the stream");

            _ms.Position = 0;

            int readCount = _ms.Read(buffer, offset, count);

            removeBytes(readCount);

            return readCount;
        }
        
        /// <summary>
        /// Read and remove the specified number of bytes in the buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read/remove.</param>
        /// <returns>A byte array.</returns>
        public byte[] ReadBytes(int count)
        {
            byte[] buffer = new byte[count];

            Read(buffer, 0, count);

            return buffer;
        }

        /// <summary>
        /// Read and remove the specified number of characters in the buffer.
        /// </summary>
        /// <param name="charCount">The number of characters to read/remove.</param>
        /// <returns>A string.</returns>
        public string ReadString(int charCount)
        {
            //byte[] b = _ms.GetBuffer();
            //char[] c = new char[charCount];

            //int bytesToReadcount;
            //if (_encoding is UnicodeEncoding)
            //    bytesToReadcount = charCount * 2; // Each char is at least 2 bytes large.
            //else
            //    bytesToReadcount = charCount; // Each char is at least 1 byte large.


            //int readCount = 0;
            //while (readCount < charCount)
            //{
            //    // Convert the bytes to a char array.
            //    readCount = _encoding.GetChars(b, 0, bytesToReadcount, c, 0);

            //    // If not enough chars were read then increment
            //    if (readCount < charCount)
            //        bytesToReadcount++;
            //}

            //removeBytes(bytesToReadcount);

            //return c.ToString();

            char[] buffer = new char[charCount];
            int readCount = ReadChars(buffer, 0, charCount);
            if (readCount < charCount)
                throw new ArgumentOutOfRangeException("there are not enough bytes in the stream");

            return buffer.ToString();
        }

        /// <summary>
        /// Read and remove the specified number of characters in the buffer.
        /// </summary>
        /// <param name="count">The number of characters to read/remove.</param>
        /// <returns>A character array.</returns>
        public int ReadChars(char[] buffer, int index, int count)
        {
            // This function was copied (and then changed) from BinaryReader.ReadChars() using Reflector.

            int num2 = 0; // the # of bytes to read at each loop
            int num3 = count; // The # of characters that still need to be read. Counts down to 0.
            int totalBytesRead = 0;
            
            _ms.Position = 0; // alreays read from the beginning of the stream.

            while (num3 > 0) // while we still need to read more characters
            {
                num2 = num3;
                //if (this.m_2BytesPerChar)
                if (_encoding is UnicodeEncoding)
                    num2 = num2 << 1;
                
                if (num2 > 0x80)
                    num2 = 0x80;

                int position = (int)_ms.Position;
                num2 = internalEmulateRead(num2);
                if (num2 == 0)
                {
                    removeBytes(totalBytesRead);
                    return (count - num3);
                }
                int num = _encoding.GetChars(_ms.GetBuffer(), position, num2, buffer, index);

                num3 -= num;
                index += num;

                totalBytesRead += num2;
            }

            removeBytes(totalBytesRead);

            return count;
        }
        private int internalEmulateRead(int count)
        {
            int num = (int)_ms.Length;
            if (num > count)
                num = count;

            if (num < 0)
                num = 0;

            _ms.Position += num;
            return num;
        }


 

 

        /// <summary>
        /// Reports the index of the position of the first occurrence of the specified byte sequence in the buffer.
        /// </summary>
        /// <param name="findme">The byte sequence to search for.</param>
        /// <returns>The zero-based index position of value if that byte sequence is found, or -1 if it is not.</returns>
        public long IndexOf(byte[] findme)
        {
            return indexOf(_ms, findme, 0, -1);
        }
        /// <summary>
        /// Reports the index of the first occurrence of the specified byte sequence in the buffer. The search starts at a specified byte position.
        /// </summary>
        /// <param name="findme">The byte sequence to search for.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of value if that byte sequence is found, or -1 if it is not.</returns>
        public long IndexOf(byte[] findme, int startIndex)
        {
            return indexOf(_ms, findme, startIndex, -1);
        }
        /// <summary>
        /// Reports the index of the first occurrence of the specified byte sequence in the buffer. The search starts at a specified byte position and examines a specified number of byte positions.
        /// </summary>
        /// <param name="findme">The byte sequence to search for.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of byte positions to examine. </param>
        /// <returns>The zero-based index position of value if that byte sequence is found, or -1 if it is not.</returns>
        public long IndexOf(byte[] findme, int startIndex, int count)
        {
            return indexOf(_ms, findme, startIndex, count);
        }
        /// <summary>
        /// Reports the index of the first occurrence of the specified String in the buffer.
        /// </summary>
        /// <param name="findme">The String to search for.</param>
        /// <returns>The zero-based index position of value if that string is found, or -1 if it is not.</returns>
        public long IndexOf(string findme)
        {
            byte[] b = _encoding.GetBytes(findme);

            return indexOf(_ms, b, 0, -1);
        }
        /// <summary>
        /// Reports the index of the first occurrence of the specified String in the buffer. The search starts at a specified byte position.
        /// </summary>
        /// <param name="findme">The String to search for.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of value if that string is found, or -1 if it is not.</returns>
        public long IndexOf(string findme, int startIndex)
        {
            byte[] b = _encoding.GetBytes(findme);

            return indexOf(_ms, b, startIndex, -1);
        }
        /// <summary>
        /// Reports the index of the first occurrence of the specified String in the buffer. The search starts at a specified byte position and examines a specified number of byte positions.
        /// </summary>
        /// <param name="findme">The String to search for.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of byte positions to examine. </param>
        /// <returns>The zero-based index position of value if that string is found, or -1 if it is not.</returns>
        public long IndexOf(string findme, int startIndex, int count)
        {
            byte[] b = _encoding.GetBytes(findme);

            return indexOf(_ms, b, startIndex, count);
        }

        private static long indexOf(Stream stream, byte[] findme, int startIndex, int count)
        {
            if (startIndex >= stream.Length)
                throw new ArgumentOutOfRangeException("startIndex is past the end of the file.");

            long prevPosition = stream.Position;

            stream.Position = startIndex;

            long endposition;
            if (count == -1)
                endposition = stream.Length - 1;
            else
                endposition = startIndex + count;

            if (endposition >= stream.Length)
                throw new ArgumentOutOfRangeException("count is past the end of the file.");

            while (stream.Position <= endposition)
            {
                // Remember our position.
                long position = stream.Position;


                // Starting from our current position, scan to determine if we match.
                int i = 0;
                int thebyte = stream.ReadByte();
                while (thebyte != -1 && thebyte == findme[i])
                {
                    i++;
                    if (i == findme.Length)
                    {
                        stream.Position = prevPosition; // reset position to same as when we entered this method.
                        return position;
                    }
                    thebyte = stream.ReadByte();
                }


                // We didn't match, so reset the position to the next byte position after our scan.
                stream.Position = position + 1;
            }

            // There were no matches in the file.
            stream.Position = prevPosition; // reset position to same as when we entered this method.
            return -1;
        }

        private void removeBytes(int count)
        {
            if (count == 0)
                return;

            int dataSize = (int)_ms.Length - count;
            byte[] extraData = new byte[dataSize];
            _ms.Position = count;
            _ms.Read(extraData, 0, (int)dataSize);
            _ms.SetLength(0); // this also sets the position to 0.
            _ms.Write(extraData, 0, dataSize); // the position will be at the end of the stream after writing.
        }

        internal virtual Encoding CurrentEncoding // internal... the user should set this from the parent object.
        {
            //get { return _encoding; }
            set { _encoding = value; }
        }

        internal void WriteInternal(byte[] data) // internal so the user can't change the buffer
        {
            WriteInternal(data, 0, data.Length);
        }

        internal void WriteInternal(byte[] data, int startIndex, int count) // internal so the user can't change the buffer
        {
            _ms.Position = _ms.Length;
            _ms.Write(data, startIndex, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("The stream does not support writing.");
        }

        protected override void Dispose(bool disposing)
        {
            if (_ms != null)
            {
                _ms.Close();
                _ms = null;
            }

            base.Dispose(disposing);
        }










        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            // do nothing
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("This stream does not support seek operations.");
            }
            set
            {
                throw new NotSupportedException("This stream does not support seek operations.");
            }
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This stream does not support seek operations.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This stream does not support seek operations.");
        }
    }











}
