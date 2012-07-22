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
using System.IO.Ports;
using System.Collections.Specialized;
using System.Collections;
using System.Xml;
using System.Threading;
using System.IO;

namespace Prophecy.Communication
{
    // Extensive Serial Port descriptions: http://www.innovatic.dk/knowledg/SerialCOM/SerialCOM.htm
	// Good examples: http://msmvps.com/blogs/coad/archive/2005/03/23/SerialPort-_2800_RS_2D00_232-Serial-COM-Port_2900_-in-C_2300_-.NET.aspx

    public class SerialCommunication : BaseCommunication
    {
        private SerialPort _port;

        private string _serialPortName;
        private int _baudRate = 9600;
        private Parity _parity = Parity.None;
        private int _dataBits = 8;
        private StopBits _stopBits = StopBits.One;
        private Handshake _handShake;
        private bool _dtrEnable = false;
        private bool _rtsEnable = false;


        ///// <summary>Occurs when the component is disposed by a call to the Dispose method.</summary>
        //public event EventHandler Disposed;


        /// <summary>
        /// Initializes a new instance of the SerialCommunication class. using 9600, 8, N, 1.
        ///  A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        public SerialCommunication()
            : this(null, 9600, Parity.None, 8, StopBits.One, Handshake.None, false, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SerialCommunication class using the specified serial port name.
        ///  A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="serialPortName">The serial port to use (for example, COM1).</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">One of the Parity values.</param>
        /// <param name="dataBits">The data bits value. Usually 8.</param>
        /// <param name="stopBits">One of the StopBits values. Usually 1.</param>
        public SerialCommunication(string serialPortName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : this (serialPortName, baudRate, parity, dataBits, stopBits, Handshake.None, false, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SerialCommunication class using the specified serial port name.
        ///  A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="serialPortName">The serial port to use (for example, COM1).</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">One of the Parity values.</param>
        /// <param name="dataBits">The data bits value. Usually 8.</param>
        /// <param name="stopBits">One of the StopBits values. Usually 1.</param>
        /// <param name="handShake">The handshake protocol. Usually none.</param>
        public SerialCommunication(string serialPortName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handShake, bool dtrEnable, bool rtsEnable)
        {
            _serialPortName = serialPortName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _handShake = handShake;
            _dtrEnable = dtrEnable;
            _rtsEnable = rtsEnable;
        }

        /// <summary>
        /// Opens a new Serial connection using the specified serial port name.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The specified port is open.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// One or more of the properties for this instance are invalid. For example, the Parity, DataBits, or Handshake properties are not valid values; the BaudRate is less than or equal to zero; the ReadTimeout or WriteTimeout property is less than zero and is not InfiniteTimeout. 
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The port name does not begin with "COM". 
        /// - or -
        /// The file type of the port is not supported.
        /// </exception>
        /// <exception cref="T:System.IOException">
        /// The port is in an invalid state. 
        /// - or - 
        /// An attempt to set the state of the underlying port failed. For example, the parameters passed from this SerialPort object were invalid.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">Access is denied to the port.</exception>
        /// <remarks>
        /// Only one open connection can exist per SerialCommunication object.
        /// The best practice for any application is to wait for some amount of time after calling the Close method before attempting to call the Open method, as the underlying serial port may not be closed instantly.
        /// </remarks>
        public void Open(string serialPortName)
        {
            _serialPortName = serialPortName;
            base.Open();
        }

        /// <summary>
        /// Opens a new SerialCommunication connection.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The specified port is open.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// One or more of the properties for this instance are invalid. For example, the Parity, DataBits, or Handshake properties are not valid values; the BaudRate is less than or equal to zero; the ReadTimeout or WriteTimeout property is less than zero and is not InfiniteTimeout. 
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// The port name does not begin with "COM". 
        /// - or -
        /// The file type of the port is not supported.
        /// </exception>
        /// <exception cref="T:System.IOException">
        /// The port is in an invalid state. 
        /// - or - 
        /// An attempt to set the state of the underlying serial port failed.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">Access is denied to the port.</exception>
        /// <remarks>
        /// Only one open connection can exist per SerialCommunication object.
        /// The best practice for any application is to wait for some amount of time after calling the Close method before attempting to call the Open method, as the underlying serial port may not be closed instantly.
        /// </remarks>
        public override void Open()
        {
            // This method is only hear so that the user can see intellisense documentation specific to this class.
            base.Open();
        }

        protected override void open()
        {
            lock (dispose_lock)
            {
                // If port is already open then return.
                if (_port != null && _port.IsOpen == true)
                    return; // port is already open.

                // If the port exists but is closed then make sure it is disposed before creating a new port... this should never happen but I suppose could in a race condition.
                if (_port != null && _port.IsOpen == false)
                {
                    try
                    {
                        _port.Dispose();
                    }
                    catch
                    {
                        // do nothing
                    }
                    finally
                    {
                        _port = null;
                    }
                }


                _port = new SerialPort(_serialPortName, _baudRate, _parity, _dataBits, _stopBits);
                _port.Handshake = _handShake;
                _port.DtrEnable = _dtrEnable;
                _port.RtsEnable = _rtsEnable;
                _port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

                try
                {
                    _port.Open();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to open serial port '" + _serialPortName + "'. " + ex.ToString());

                    try
                    {
                        _port.Dispose();
                    }
                    catch
                    {
                        // do nothing
                    }
                    finally
                    {
                        _port = null; // we must set _port to null because the Connected property looks at it.
                    }
                    throw;
                }
            }
        }

        protected override void OnConnectionAttempt()
        {
            // Since the serial port connection stays open even when the hardware is unplugged,
            // don't try to reopen the connection unless the actual port is not open.
            if (_port == null || _port.IsOpen == false)
            {
                // Call the base OnConnectionAttempt() method which in turn calls Open() and possibly OnConnectionEstablished().
                base.OnConnectionAttempt();
            }
            else
            {
                // Send the test command so that we may receive a response to indicate the hardware is connected again.
                try
                {
                    if (ConnectionMonitorTestBytes != null && ConnectionMonitorTestBytes.Length > 0)
                    {
                        //#if DEBUG
                        //if (ConnectionMonitorTestRequest != null && ConnectionMonitorTestRequest.Length > 0)
                        //    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  _connectionMonitorTimer_Elapsed() sending: " + ConnectionMonitorTestRequest.Replace("\r", "<CR>").Replace("\n", "<LF>"));
                        //else
                        //    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  " + System.Threading.Thread.CurrentThread.GetHashCode() + "  _connectionMonitorTimer_Elapsed() sending: " + global::Common.SystemUtility.ByteArrayToHexString(ConnectionMonitorTestBytes, " "));
                        //#endif

                        Send(ConnectionMonitorTestBytes);
                    }
                }
                catch (Exception ex)
                {
                    // just catch the error in and continue
                }

                OnConnectionMonitorTest();
            }
        }

        /// <summary>
        /// Gets or sets the name of the serial port used for communications.
        /// </summary>
        public string SerialPortName
        {
            get { return _serialPortName; }
            set { _serialPortName = value; }
        }

        private readonly object port_DataReceivedLockObject = new object();
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (port_DataReceivedLockObject)
            {
                try
                {
                    //TODO:OnLog("port_DataReceived() Entered.  Event Type: " + e.EventType.ToString() + " v-v-v-v-v");

                    SerialPort port = sender as SerialPort;

                    //TODO:OnLog("port_DataReceived() port is null = " + (port == null).ToString());

                    //log("port_DataReceived() BytesToRead = " + port.BytesToRead);

                    //string s = port.ReadExisting();


                    byte[] buffer = new byte[1024];
                    int bytesRead = port.Read(buffer, 0, buffer.Length);  // TODO: Read() and ReadExisting() sometimes totally hard crashes the application.  Not sure if this is just a Vista x64 thing or a bad serial port since it hasn't been reported by other people.

                    _logger.Debug("SerialCommunication received " + bytesRead + " bytes.", buffer, 0, bytesRead);

                    ProcessReceivedData(buffer, bytesRead);
                }
                catch (System.IO.IOException ex)
                {
                    // Ignore the error if the port was closed before the read finished.
                    // I added this code here because I have seen the following exception when the stack trace shows _port.Read() is called and I suspect that the port is closing during the read.
                    // System.IO.IOException: The I/O operation has been aborted because of either a thread exit or an application request.
                    if (_port == null || _port.IsOpen == false)
                        return;
                    else
                        //throw; // Do not throw the error since it will be on it's own thread and may cause the app to crash.
                        _logger.Error("An IOException error occurred while receiving serial data.", ex);
                }
                catch (Exception ex)
                {
                    //TODO:OnLog("port_DataReceived() EXCEPTION!! " + Utility.GetExceptionMessages(ex) + "");
                    // throw; // Do not throw the error since it will be on it's own thread and may cause the app to crash.

                    _logger.Error("An error occurred while receiving serial data.", ex);
                }
            }
        }


        private readonly object _sendRawLockObject = new object();
        protected override void sendData(byte[] raw)
        {
            throwExceptionIfClosed();

            if (raw == null)
                throw new ArgumentNullException("raw");

            try
            {
                lock (_sendRawLockObject)
                {
                    _logger.Debug("SerialCommunication sending ", raw);
                    _port.Write(raw, 0, raw.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while sending serial data. ", raw, ex);
            }
        }

        ///// <summary>
        ///// Send the hexidecimal string as data.
        ///// </summary>
        ///// <param name="hexData">The binary data represented as a contiguous string of hexidecimal characters. Example: \"00FF0900\"</param>
        //public override void SendBinary(string hexData)
        //{
        //    byte[] b = base.HexStringToByteArray(hexData);

        //    SendRaw(b);
        //}
        //public override void SendBinary(string hexData, int delayMilliseconds)
        //{
        //    byte[] b = base.HexStringToByteArray(hexData);

        //    SendRaw(b, delayMilliseconds);
        //}

        private void throwExceptionIfClosed()
        {
            lock (dispose_lock)
            {
                if (_port == null || _port.IsOpen == false)
                    throw new ObjectDisposedException("_port", "The serial port is closed.");
            }
        }

        /// <summary>
        /// Indicates if the pc is successfully communicating with the the serial device.
        /// 
        /// If connection monitoring is disabled this will always be true as long as the serial port was successfully opened.
        /// If connection monitoring is enabled, this will only be true if the serial port was successfully opened and the device consistantly sends data to the pc within a specified time limit.
        /// </summary>
        public override bool Connected
        {
            get
            {
                if (base.ConnectionMonitorEnabled)
                    return _port != null && _port.IsOpen && base.Connected;
                else
                    return _port != null && _port.IsOpen;
            }
        }









       

        /// <summary>
        /// Gets or sets the serial baud rate.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The baud rate specified is less than or equal to zero, or is greater than
        /// the maximum allowable baud rate for the device.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        public int BaudRate
        {
            get { return _port.BaudRate; }
            set { _port.BaudRate = value; }
        }

        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or -An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///  The data bits value is less than 5 or more than 8.
        /// </exception>
        public int DataBits
        {
            get { return _port.DataBits; }
            set { _port.DataBits = value; }
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The System.IO.Ports.SerialPort.Parity value passed is not a valid value in
        /// the System.IO.Ports.Parity enumeration.
        /// </exception>
        public Parity Parity
        {
            get { return _port.Parity; }
            set { _port.Parity = value; }
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The value of the System.IO.Ports.SerialPort.RtsEnable property was set or
        /// retrieved while the System.IO.Ports.SerialPort.Handshake property is set
        /// to the System.IO.Ports.Handshake.RequestToSend value or the System.IO.Ports.Handshake.RequestToSendXOnXOff
        /// value.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        public StopBits StopBits
        {
            get { return _port.StopBits; }
            set { _port.StopBits = value; }
        }

        /// <summary>
        /// Gets or sets the handshaking protocol for serial port transmission of data.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The value passed is not a valid value in the System.IO.Ports.Handshake enumeration.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open()
        /// method has not been called or the System.IO.Ports.SerialPort.Close() method
        /// has been called.
        /// </exception>
        public Handshake Handshake
        {
            get { return _handShake; }
            set
            {
                _handShake = value;
                if (_port != null)
                    _port.Handshake = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Request to Send (RTS) signal is enabled during serial communication.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// The value of the System.IO.Ports.SerialPort.RtsEnable property was set or
        /// retrieved while the System.IO.Ports.SerialPort.Handshake property is set
        /// to the System.IO.Ports.Handshake.RequestToSend value or the System.IO.Ports.Handshake.RequestToSendXOnXOff
        /// value.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        public bool RtsEnable
        {
            get { return _rtsEnable; }
            set
            {
                _rtsEnable = value;
                if (_port != null)
                    _port.RtsEnable = value;
            }
        }

        /// <summary>
        /// Gets the state of the Clear-to-Send line.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open()
        /// method has not been called or the System.IO.Ports.SerialPort.Close() method
        /// has been called.
        /// </exception>
        public bool CtsHolding
        {
            get { return _port.CtsHolding; }
        }

        /// <summary>
        /// Gets the state of the Data Set Ready (DSR) signal.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open()
        /// method has not been called or the System.IO.Ports.SerialPort.Close() method
        /// has been called.
        /// </exception>
        public bool DsrHolding
        {
            get { return _port.DsrHolding; }
        }

        /// <summary>
        /// Gets or sets a value that enables the Data Terminal Ready (DTR) signal during serial communication.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        /// The port is in an invalid state. - or - An attempt to set the state of the
        /// underlying port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        /// object were invalid.
        /// </exception>
        public bool DtrEnable
        {
            get { return _dtrEnable; }
            set
            {
                _dtrEnable = value;
                if (_port != null)
                    _port.DtrEnable = value;
            }
        }




        private readonly object dispose_lock = new object();

        /// <summary>
        /// Releases all resources used by the SerialCommunication.
        /// </summary>
        public override void Dispose()
        {
            lock (dispose_lock)
            {
                try
                {
                    base.Dispose();
                }
                catch
                {
                }

                try
                {
                    if (_port != null)
                    {
                        _port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                        _port.Dispose(); // We may gave see this throw an error if the serial port no longer exists such as if a usb<->serial adapter was removed... but I'm not sure this is where it was.
                    }
                }
                catch
                {
                }
                finally
                {
                    _port = null;
                }
            }
        }

        /// <summary>
        /// Simulate data being received from the serial port. The data will be processed as if it were actually received as incoming data from the serial ports. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(string data)
        {
            SimulateReceivedData(CurrentEncoding.GetBytes(data));
        }

        /// <summary>
        /// Simulate data being received from the serial port. The data will be processed as if it were actually received as incoming data from the serial ports. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(byte[] data)
        {
            lock (port_DataReceivedLockObject)
            {
                try
                {
                    _logger.Debug("SerialCommunication simulating received " + data.Length + " bytes.", data);

                    ProcessReceivedData(data, data.Length);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occurred while simulating received serial data.", ex);
                }
            }
        }

        public override string ConnectionDisplayText
        {
            get { return _serialPortName; }
        }
    }
}
