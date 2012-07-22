/* **********************************************************************************
 * Copyright (c) 2011 John Hughes
 *
 * Prophecy is licenced under the Microsoft Reciprocal License (Ms-RL).
 *
 * Project Website: http://prophecy.codeplex.com/
 * **********************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Prophecy.Extensions;

namespace Prophecy.Communication
{
    [Obsolete]
    public class TcpClientCommunication : BaseCommunication
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream; // result of _tcp.GetStream()
        private byte[] _readBuffer;

        private string _tcpHostName;  // hostname of the elk network interface
        private int _tcpPort; // tcp port


        //private Timer _readTimer;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the TcpClientCommunication class.
        /// A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        public TcpClientCommunication(string hostName, int port)
        {
            _tcpHostName = hostName;
            _tcpPort = port;
        }


        /// <summary>
        /// Create a TcpClientCommunication object using an already open TcpClient.
        /// </summary>
        public TcpClientCommunication(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            // Since images are passed between the apps, make sure the receive buffer is big enough! It probably just needs to be the same size as the SendBufferSize but I'm not sure.
            _tcpClient.ReceiveBufferSize = 64 * 1024; // this propery can not be set on a PocketPC and defaults to 32768, otherwise it defaults to 8192.
            _readBuffer = new byte[_tcpClient.ReceiveBufferSize];

            // Make sure the MaximumReadBufferSize is at least as bif as the TcpClient's ReceiveBufferSize otherwise we will overflow when we receive a huge packet.
            if (MaximumReadBufferSize < _tcpClient.ReceiveBufferSize)
                MaximumReadBufferSize = _tcpClient.ReceiveBufferSize;

            // Start asynchronous read
            try
            {
                // BeginRead() will throw an IOException if the underlying Socket is closed, or there was a failure while reading from the network, or an error occurred when accessing the socket.
                _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, new AsyncCallback(readCallback), null);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Opens a new TCP connection using the specified address and port.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        /// <exception cref="System.ArgumentNullException">The hostname parameter is nullNothingnullptra null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The port parameter is not between MinPort and MaxPort.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">An error occurred when accessing the socket. use SocketException.ErrorCode to obtain the specific error code. After you have obtained this code, you can refer to the Windows Sockets version 2 API error code documentation in MSDN for a detailed description of the error.</exception>
        /// <remarks>
        /// This makes a synchronous connection attempt to the provided host name and port number and will block until it either connects or fails. The underlying service provider will assign the most appropriate local IP address and port number.
        /// </remarks>
        public void Open(string hostName, int port)
        {
            _tcpHostName = hostName;
            _tcpPort = port;

            base.Open();
        }

        /// <summary>
        /// Opens a new TCP connection using the address and port specified in the constructor.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">The hostname parameter is nullNothingnullptra null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The port parameter is not between MinPort and MaxPort.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">An error occurred when accessing the socket. use SocketException.ErrorCode to obtain the specific error code. After you have obtained this code, you can refer to the Windows Sockets version 2 API error code documentation in MSDN for a detailed description of the error.</exception>
        /// <remarks>
        /// This makes a synchronous connection attempt to the provided host name and port number and will block until it either connects or fails. The underlying service provider will assign the most appropriate local IP address and port number.
        /// </remarks>
        public override void Open()
        {
            // This method is only hear so that the user can see intellisense documentation specific to this class.
            base.Open();
        }
        
        protected override void open()
        {
            if (_tcpClient == null || _tcpClient.Client.Connected == false)
            {
                _isDisposed = false;

                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  %%%%%%%%%%%%%%%%%% TcpClientCommunication.Open.  Attempting to connect to " + _tcpHostName + ":" + _tcpPort + ".");

                try
                {
                    _tcpClient = new TcpClient(_tcpHostName, _tcpPort);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to connect via tcp to '" + _tcpHostName + ":" + _tcpPort + ". " + ex.ToString(true));
                    throw;
                }

                _stream = _tcpClient.GetStream();
                // Since images are passed between the apps, make sure the receive buffer is big enough! It probably just needs to be the same size as the SendBufferSize but I'm not sure.
                _tcpClient.ReceiveBufferSize = 64 * 1024; // this propery can not be set on a PocketPC and defaults to 32768, otherwise it defaults to 8192.
                _readBuffer = new byte[_tcpClient.ReceiveBufferSize];

                // Make sure the MaximumReadBufferSize is at least as bif as the TcpClient's ReceiveBufferSize otherwise we will overflow when we receive a huge packet.
                if (MaximumReadBufferSize < _tcpClient.ReceiveBufferSize)
                    MaximumReadBufferSize = _tcpClient.ReceiveBufferSize;


                try
                {
                    // auto test the connection so we get an exception... not sure how to make this work
                    // http://forums.msdn.microsoft.com/en-US/netfxnetcom/thread/d5b6ae25-eac8-4e3d-9782-53059de04628/
                    // http://msdn.microsoft.com/en-us/library/ms741621.aspx
                    //setKeepAlive(_tcpClient.Client, 5000, 5000);
                    //_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    ////_tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, 1);
                }
                catch (Exception)
                {
                }

                // Start asynchronous read
                try
                {
                    // BeginRead() will throw an IOException if the underlying Socket is closed, or there was a failure while reading from the network, or an error occurred when accessing the socket.
                    _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, new AsyncCallback(readCallback), null);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        void readCallback(IAsyncResult ar)
        {
            int unprocessedByteCount = 0;
            int byteCount = -1;
            try
            {
                if (_stream != null)
                {
                    byteCount = _stream.EndRead(ar);
                    if (byteCount > 0)
                    {
                        _logger.Debug("TcpClientCommunication received " + byteCount + " bytes.", _readBuffer, 0, byteCount);
                        //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  TcpClientCommunication received " + byteCount + " bytes. hex: " + base.ByteArrayToHexString(_readBuffer, 0, byteCount, " "));

                        // Copy the data to a new buffer that will be passed to ProcessReceivedData().
                        //byte[] buffer = new byte[byteCount];
                        //Array.Copy(_readBuffer, buffer, byteCount);

                        // Process the received data.
                        ProcessReceivedData(_readBuffer, byteCount);
                    }
                    else
                    {
                        // The connection was dropped!
                        if (this.ConnectionMonitorEnabled)
                            OnConnectionLost();
                    }
                }
            }
            catch (Exception ex)
            {
                bool logTheException = true;

                // Don't log the exception if the connection was forcably closed.
                for (Exception ex2 = ex; ex2 != null; ex2= ex2.InnerException)
                {
                    SocketException se = ex2 as SocketException; // The SocketException is usually the inner exception of an IOException.
                    if (se != null && se.ErrorCode == 10054) // Connection reset by peer. An existing connection was forcibly closed by the remote host. This normally results if the peer application on the remote host is suddenly stopped, the host is rebooted, the host or remote network interface is disabled, or the remote host uses a hard close.
                    {
                        logTheException = false;
                        break;
                    }
                }

                if (logTheException)
                    _logger.Error("An error ocurred in the tcp ip driver read callback.", ex);
            }
            finally
            {
                if (_tcpClient != null && _stream != null && byteCount > 0) // since we now lock() this section, the stream should always be there.
                {
                    try
                    {
                        // BeginRead() will throw an IOException if the underlying Socket is closed, or there was a failure while reading from the network, or an error occurred when accessing the socket.
                        _stream.BeginRead(_readBuffer, unprocessedByteCount, _readBuffer.Length - unprocessedByteCount, new AsyncCallback(readCallback), null);
                    }
                    catch (Exception ex)
                    {
                        if (_isDisposed == false) // Check if the error occurred because this object was disposed. This can easily happen in readCallback()->OnConnectionLost().
                        {
                            if (this.ConnectionMonitorEnabled)
                                OnConnectionLost();
                            else
                                _logger.Error("An error ocurred in the tcp ip driver read callback when attempting a Begin read on the network stream.  The connection is assumed to be lost.", ex);
                        }
                    }
                    
                }
            }
        }
        //TODO: BeginRead throws IOException - when the connection is lost.

        /// <summary>
        /// Indicates if the pc is successfully communicating via tcp.
        /// 
        /// If connection monitoring is disabled this will only be true as long a tcp connection was established and sending data does not cause an error.
        /// If connection monitoring is enabled this will only be true if a tcp connection was established, sending data does not cause an error, and the device consistantly sends data to the pc within a specified time limit.
        /// </summary>
        public override bool Connected
        {
            get
            {
                return _tcpClient != null && _tcpClient.Client.Connected && base.Connected;
            }
        }

        protected override void OnConnectionLost()
        {
            cleanUp();

            base.OnConnectionLost();
        }


        /// <summary>
        /// Releases all resources used by the TcpClientCommunication.
        /// </summary>
        public override void Dispose()
        {
			_isDisposed = true;
			
            try
            {
                base.Dispose();

                cleanUp();
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        private void cleanUp()
        {
			_isDisposed = true;

            if (_tcpClient != null)
            {
                _tcpClient.Close();

                _tcpClient = null;
            }

            // Closing the TcpClient instance does not close the network stream, so do it manually.
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }

        //private bool setKeepAlive(Socket sock, ulong time, ulong interval)
        //{
        //    const int bytesperlong = 4; // 32 / 8
        //    const int bitsperbyte = 8;

        //    try
        //    {
        //        // resulting structure
        //        byte[] SIO_KEEPALIVE_VALS = new byte[3 * bytesperlong];

        //        // array to hold input values
        //        ulong[] input = new ulong[3];

        //        // put input arguments in input array
        //        if (time == 0 || interval == 0) // enable disable keep-alive
        //            input[0] = (0UL); // off
        //        else
        //            input[0] = (1UL); // on

        //        input[1] = (time); // time millis
        //        input[2] = (interval); // interval millis

        //        // pack input into byte struct
        //        for (int i = 0; i < input.Length; i++)
        //        {
        //            SIO_KEEPALIVE_VALS[i * bytesperlong + 3] = (byte)(input[i] >> ((bytesperlong - 1) * bitsperbyte) & 0xff);
        //            SIO_KEEPALIVE_VALS[i * bytesperlong + 2] = (byte)(input[i] >> ((bytesperlong - 2) * bitsperbyte) & 0xff);
        //            SIO_KEEPALIVE_VALS[i * bytesperlong + 1] = (byte)(input[i] >> ((bytesperlong - 3) * bitsperbyte) & 0xff);
        //            SIO_KEEPALIVE_VALS[i * bytesperlong + 0] = (byte)(input[i] >> ((bytesperlong - 4) * bitsperbyte) & 0xff);
        //        }
        //        // create bytestruct for result (bytes pending on server socket)
        //        byte[] result = BitConverter.GetBytes(0);
        //        // write SIO_VALS to Socket IOControl
        //        sock.IOControl(IOControlCode.KeepAliveValues, SIO_KEEPALIVE_VALS, result);
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //    return true;
        //}


        //void _readTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    Timer readTimer = (Timer)sender;

        //    try
        //    {
        //        readTimer.Stop();

        //        if (_tcpClient != null) // only read if we are connected
        //        {
        //            //if (_stream.DataAvailable)
        //            //{
        //                // Read from stream
        //                byte[] buffer = Utility.ReadNetworkStreamFully(_stream);
        //                if (buffer.Length > 0)
        //                {
        //                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  " + "TcpClientCommunication received: " + base.ByteArrayToHexString(buffer, " "));

        //                    ProcessReceivedData(buffer, buffer.Length);
        //                }
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log("An error ocurred in the tcp ip driver read timer event.  Error:" + Utility.GetExceptionMessages(ex), LoggerSeverity.Error);
        //    }
        //    finally
        //    {
        //        readTimer.Start();
        //    }
        //}

        
        private readonly object _sendRawLockObject = new object();
        protected override void sendData(byte[] data)
        {
            if (data == null)
            {
                _logger.Error("Data can not be sent via tcp because there the data is null.");
                return;
            }

            if (_tcpClient == null || _stream == null)
            {
                _logger.Error("Data can not be sent via tcp because there is currently no connection. Data Length: " + data.Length, data, 0, Math.Min(data.Length, 200));
                return;
            }

            try
            {
                lock (_sendRawLockObject)
                {
                    _logger.Debug("TcpClientCommunication sending " + data.Length, data, 0, Math.Min(data.Length, 200));
                    
                    _stream.Write(data, 0, data.Length); // NOTE: MAKE SURE THE TcpClient.ReceiveBufferSize is big enough on the receiver side for the data sent!!!

                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  TcpClientCommunication finished sending " + data.Length + " bytes.");
                }
            }
            catch (System.IO.IOException ex)
            {
                // The connection dropped so the data could not be written.
                OnConnectionLost();
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while sending data via tcp: " + data, ex);
            }
        }

        public override void Send(byte[] data)
        {
            // Override the base Send() to detect if there is a connection. The string methods do not need to be overridden since they will end up here.
            if (Connected == false)
                throw new Exception("Data cannot be sent since there is no tcp connection established with " + _tcpHostName + ":" + _tcpPort);

            base.Send(data);
        }
        public override void Send(byte[] data, int delayMilliseconds)
        {
            // Override the base Send() to detect if there is a connection. The string methods do not need to be overridden since they will end up here.
            if (Connected == false)
                throw new Exception("Data cannot be sent since there is no tcp connection established with " + _tcpHostName + ":" + _tcpPort);

            EnqueueDataToSend(data, delayMilliseconds);
        }

        /// <summary>
        /// Simulate data being received from the tcp connection. The data will be processed as if it were actually received as incoming data from the tcp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(string data)
        {
            SimulateReceivedData(CurrentEncoding.GetBytes(data));
        }

        /// <summary>
        /// Simulate data being received from the tcp connection. The data will be processed as if it were actually received as incoming data from the tcp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(byte[] data)
        {
            try
            {
                _logger.Debug("TcpClientCommunication simulating received " + data.Length + " bytes.", data);

                ProcessReceivedData(data, data.Length);
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while simulating received tcp data.", ex);
            }
        }
        
        public override string ConnectionDisplayText
        {
            get 
            {
                if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.RemoteEndPoint != null)
                    return _tcpClient.Client.RemoteEndPoint.ToString();
                else
                    return _tcpHostName + ":" + _tcpPort;
            }
        }

    }
}
