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
using System.Net;
using System.Threading;

namespace Prophecy.Communication
{
    // Silverlight compatible TcpCommunication - This class use the asynchronous calls because Silverlight does not support synchronous calls.

    // References:
    // http://msdn.microsoft.com/en-us/library/system.net.sockets.socket_members(v=VS.95).aspx
    // http://weblogs.asp.net/mschwarz/archive/2008/03/07/silverlight-2-and-sockets.aspx
    // http://weblogs.asp.net/dwahlin/archive/2008/04/13/pushing-data-to-a-silverlight-client-with-sockets-part-ii.aspx

    /// <summary>
    /// The TcpTcpCommunication class provides a Silverlight compatible tcp client. Silverlight does not support the TcpClient class so this class was created to use Sockets which Silverlight does support.
    /// </summary>
    public class TcpCommunication : BaseCommunication
    {
        private Socket _socket;
        private byte[] _readBuffer;

        private string _tcpHostName;  // hostname
        private int _tcpPort; // tcp port

        private IPEndPoint _localEndPoint = null;


        private AutoResetEvent _autoResetEventConnection;
        private AutoResetEvent _autoResetEventSend;

        const int READBUFFERSIZE = 4 * 1024;

        private SocketAsyncEventArgs _connectAndReceiveArgs;

        /// <summary>
        /// Initializes a new instance of the TcpCommunication class.
        /// A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        public TcpCommunication(string hostName, int port) : this(hostName, port, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TcpCommunication class.
        /// A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        /// <param name="localEndPoint">The local EndPoint to associate with the Socket.</param>
        public TcpCommunication(string hostName, int port, IPEndPoint localEndPoint)
        {
            _tcpHostName = hostName;
            _tcpPort = port;
            _localEndPoint = localEndPoint;
        }

        /// <summary>
        /// Create a TcpCommunication object using an already open Socket.
        /// </summary>
        public TcpCommunication(Socket socket)
        {
            _socket = socket;

            _autoResetEventConnection = new AutoResetEvent(false);
            _autoResetEventSend = new AutoResetEvent(false);

            _readBuffer = new byte[READBUFFERSIZE];

            // Start asynchronous read
            try
            {
                _connectAndReceiveArgs = new SocketAsyncEventArgs();
                _connectAndReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                _connectAndReceiveArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
                _connectAndReceiveArgs.UserToken = _socket;
                if (socket.ReceiveAsync(_connectAndReceiveArgs) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                    _socket_ReceivedData(socket, _connectAndReceiveArgs); // I guess this can happen, but it has the potential for a stack overflow error due to recursion if every call is synchronous... but I have never seen a synchronous call here.
            }
            catch (Exception ex)
            {
                // On failure free up the SocketAsyncEventArgs. On success it is reused for receiving.
                if (_connectAndReceiveArgs != null)
                {
                    _connectAndReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                    _connectAndReceiveArgs.Dispose();
                    _connectAndReceiveArgs = null;
                }

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
            if (_socket == null || _socket.Connected == false)
            {
                //_logger.Debug("TCP IP Driver is opening TCP/IP port.");

                //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  %%%%%%%%%%%%%%%%%% TcpCommunication.Open.  Attempting to connect to " + _tcpHostName + ":" + _tcpPort + ".");

                _autoResetEventConnection = new AutoResetEvent(false);
                _autoResetEventSend = new AutoResetEvent(false);

                try
                {
                    // Get the end point.
                    IPAddress ipAddress = null;
                    if (System.Net.IPAddress.TryParse(_tcpHostName, out ipAddress) == false)
                    {
                        // Find the IPv4 address in the list first.
                        foreach (IPAddress ipAddr in Dns.GetHostEntry(_tcpHostName).AddressList)
                        {
                            if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ipAddress = ipAddr;
                                break;
                            }
                        }
                        if (ipAddress == null)
                        {
                            // Find the IPv6 address in the list if no IPv4 address was found.
                            foreach (IPAddress ipAddr in Dns.GetHostEntry(_tcpHostName).AddressList)
                            {
                                if (ipAddr.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    ipAddress = ipAddr;
                                    break;
                                }
                            }
                        }
                        if (ipAddress == null)
                            throw new Exception("Failed to resolve '" + _tcpHostName + "' to an internet ip address.");
                    }
                    IPEndPoint endPoint = new IPEndPoint(ipAddress, _tcpPort);

                    _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // interNetwork is IPv4
                    
                    if (_localEndPoint != null)
                        _socket.Bind(_localEndPoint);

                    // Silverlight uses new DnsEndPoint: DnsEndPoint endPoint = new DnsEndPoint(_tcpHost, _tcpPort);

                    _connectAndReceiveArgs = new SocketAsyncEventArgs();
                    _connectAndReceiveArgs.UserToken = _socket;
                    _connectAndReceiveArgs.RemoteEndPoint = endPoint;
                    _connectAndReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ConnectCompleted);

                    if (_socket.ConnectAsync(_connectAndReceiveArgs) == true) // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                        _autoResetEventConnection.WaitOne();
                    else
                        _socket_ConnectCompleted(_socket, _connectAndReceiveArgs);

                    if (_connectAndReceiveArgs.SocketError != SocketError.Success)
                        throw new SocketException((int)_connectAndReceiveArgs.SocketError);

                    if (_socket.Connected == false) // Once I though I saw no socket error but there was no connection so I check that here.
                        throw new Exception("There was no socket error but failed to connect to remote host.");
                }
                catch (Exception ex)
                {
                    // On failure free up the SocketAsyncEventArgs. On success it is reused for receiving.
                    if (_connectAndReceiveArgs != null)
                    {
                        _connectAndReceiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ConnectCompleted);
                        _connectAndReceiveArgs.Dispose();
                        _connectAndReceiveArgs = null;
                    }

                    //_logger.Error("Failed to connect via TCP/IP.", ex);
                    //System.Diagnostics.Debug.WriteLine("Failed to connect via tcp to '" + _tcpHostName + ":" + _tcpPort + "'. " + SystemUtility.GetExceptionMessages(ex));
                    throw;
                }


            }
        }

        private void _socket_ConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    _readBuffer = new byte[READBUFFERSIZE];
                    e.SetBuffer(_readBuffer, 0, _readBuffer.Length);
                    e.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ConnectCompleted);
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                    Socket socket = (Socket)e.UserToken;
                    if (socket.ReceiveAsync(e) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                        _socket_ReceivedData(sender, e); // I guess this can happen, but it has the potential for a stack overflow error due to recursion if every call is synchronous... but I have never seen a synchronous call here.
                }

                if (_autoResetEventConnection != null)
                    _autoResetEventConnection.Set(); // we must set this even if the connection failed.
            }
            catch (Exception ex)
            {
                _logger.Error("An error ocurred in the tcp ip driver connect completed callback.", ex);
            }
        }

        private void _socket_ReceivedData(object sender, SocketAsyncEventArgs e)
        {
            // This will get called when _socket.Shutdown(both or receive) gets called and e.BytesTransferred will be 0.

            lock (cleanUp_Lock)
            {
                try
                {
                    if (_socket != null)
                    {
                        if (e.BytesTransferred > 0)
                        {
                            _logger.Debug("TcpCommunication received " + e.BytesTransferred + " bytes from " + _socket.RemoteEndPoint.ToString(), e.Buffer, e.Offset, e.BytesTransferred);
                            // commented out logging since ByteArrayToHexString	is sloooow
                            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  TcpCommunication received " + e.BytesTransferred + " bytes. hex: " + base.ByteArrayToHexString(e.Buffer, e.Offset, e.BytesTransferred, " "));

                            // Copy the data to a new buffer that will be passed to ProcessReceivedData().
                            byte[] buffer = new byte[e.BytesTransferred];
                            Array.Copy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                            // Process the received data.
                            ProcessReceivedData(buffer, buffer.Length);
                        }
                        else
                        {
                            // From docs:
                            // For byte streams, zero bytes having been read indicates graceful closure and that no more bytes will ever be read
                            //  For message-oriented sockets, where a zero byte message is often allowable, a SocketException with the SocketAsyncEventArgs.SocketError set to the native Winsock WSAEDISCON error code (10101) is used to indicate graceful closure.
                            // In any case, a SocketException with the SocketAsyncEventArgs.SocketError set to the native Winsock WSAECONNRESET error code (10054) indicates an abortive close has occurred.

                            // The connection was dropped!
                            if (e.SocketError == SocketError.TimedOut)
                                _logger.Debug("Failed to receive data via tcp, the connection was likely lost due to a network cable disconnection, lost packet, or tcp failure (in some cases such as with a GC100 this could also indicate that the remote client has stopped responding but hasn't gracefully disconnected). Socket Error: " + e.SocketError.ToString());
                            else if (e.SocketError == SocketError.ConnectionReset)
                                _logger.Debug("The remote client aborted the connection. Socket Error: " + e.SocketError.ToString());
                            else if (e.SocketError == SocketError.Disconnecting)
                                _logger.Debug("The remote client has disconnected. Socket Error: " + e.SocketError.ToString());
                            else if (e.SocketError == SocketError.Success)
                                _logger.Debug("The remote client has disconnected.");
                            else
                                _logger.Debug("Failed to receive data via tcp, a communication issue has occurred. Check the Socket Error: " + e.SocketError.ToString());
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
                    if (_socket != null && e.BytesTransferred > 0) // since we now lock() this section, the socket should always be there.
                    {
                        try
                        {
                            //Prepare to receive more data
                            Socket socket = (Socket)e.UserToken;
                            if (socket.ReceiveAsync(e) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                                _socket_ReceivedData(sender, e); // I guess this can happen, but it has the potential for a stack overflow error due to recursion if every call is synchronous... but I have never seen a synchronous call here.
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("An error occurred in the tcp ip driver read callback when attempting a receive async data on the socket.  The connection is assumed to be lost.", ex);
                            if (this.ConnectionMonitorEnabled)
                                OnConnectionLost();
                        }
                        
                    }
                }
            }
        }

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
                return _socket != null && _socket.Connected && base.Connected;
            }
        }

        protected override void OnConnectionLost()
        {
            cleanUp();

            base.OnConnectionLost();
        }


        /// <summary>
        /// Releases all resources used by the TcpCommunication.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch
            {
                // do nothing
            }

            try
            {
                cleanUp();
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        private readonly object cleanUp_Lock = new object();
        private void cleanUp()
        {
            lock (cleanUp_Lock)
            {
                // Set the auto reset events to null before disposing to prevent race condition in callback routines.
                if (_autoResetEventConnection != null)
                {
                    AutoResetEvent tmp = _autoResetEventConnection;
                    _autoResetEventConnection = null;
                    tmp.Close();
                }

                if (_autoResetEventSend != null)
                {
                    AutoResetEvent tmp = _autoResetEventSend;
                    _autoResetEventSend = null;
                    tmp.Set();
                    tmp.Close();
                }

                if (_connectAndReceiveArgs != null)
                {
                    try
                    {
                        _connectAndReceiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ConnectCompleted);
                        _connectAndReceiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                        _connectAndReceiveArgs.Dispose();
                        _connectAndReceiveArgs = null;
                    }
                    catch
                    {
                    }
                }

                if (_socket != null)
                {
                    try
                    {
                        // When using a connection-oriented Socket, always call the Shutdown method before closing the Socket. This ensures that all data is sent and received on the connected socket before it is closed.
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        _socket.Close();
                    }
                    catch
                    {
                    }

                    _socket = null;
                }
            }
        }
        
        private readonly object _sendRawLockObject = new object();
        protected override void sendData(byte[] data)
        {
            if (data == null)
            {
                _logger.Error("Data can not be sent via tcp because the data is null.");
                return;
            }

            SocketAsyncEventArgs args = null;

            try
            {
                lock (_sendRawLockObject)
                {
                    args = new SocketAsyncEventArgs();
                    bool needWait = false;

                    lock (cleanUp_Lock) // don't wrap the whole method in this lock since we want to allow cleanUp() to be called even while we are waiting for the send confirmation.
                    {
                        if (_socket == null || _socket.Connected == false)
                        {
                            _logger.Error("Data can not be sent via tcp because there is currently no connection. Remote Address: " + getRemoteAddress() + ", Data Length: " + data.Length, data, 0, Math.Min(data.Length, 200));
                            return;
                        }

                        _logger.Debug("TcpCommunication sending " + data.Length + " bytes.", data);

                        List<ArraySegment<byte>> l = new List<ArraySegment<byte>>();
                        l.Add(new ArraySegment<byte>(data));
                        args.BufferList = l;

                        args.UserToken = _socket;
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_SentData);

                        needWait = _socket.SendAsync(args); // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                    }

                    if (needWait)
                    {
                        try
                        {
                            if (_autoResetEventSend != null)
                                _autoResetEventSend.WaitOne(); // this can cause NullReference exception since this object is multi-threaded so we catch that below.
                        }
                        catch (NullReferenceException ex)
                        {
                            // ignore. this could potentially happen if _autoResetEventSend were set to null immediately after the call to SendAsync().
                            return; // return since the object was disposed
                        }
                    }

                    if (args.SocketError != SocketError.Success)
                        throw new SocketException((int)args.SocketError);

                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  TcpCommunication finished sending " + data.Length + " bytes.");
                }
            }
            catch (SocketException ex)
            {
                // The connection dropped so the data could not be written.
                _logger.Error("Failed to send data via tcp, the connection was likely lost due to a network cable disconnection, lost packet, or tcp failure. Socket Error: " + ex.SocketErrorCode.ToString(), /*data, 0, 200*/ ex); ;
                OnConnectionLost();
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while sending message via tcp. Data Length: " + data.Length, data, 0, Math.Min(data.Length, 200));
            }
            finally
            {
                if (args != null)
                {
                    args.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_SentData);
                    args.Dispose();
                    args = null;
                }
            }
        }

        private string getRemoteAddress()
        {
            try
            {
                if (_socket != null && _socket.RemoteEndPoint != null)
                    return _socket.RemoteEndPoint.ToString();
                else if (string.IsNullOrEmpty(_tcpHostName) == false)
                    return _tcpHostName + ":" + _tcpPort;
                else
                    return "<unspecified>";
            }
            catch
            {
                return "<unspecified>";
            }
        }


        private void _socket_SentData(object sender, SocketAsyncEventArgs e)
        {
            //lock (cleanUp_Lock)  WE CAN'T LOCK HERE! It causes a deadlock since sendData() locks on it too and then waits.
            //{
            try
            {
                if (_autoResetEventSend != null)
                    _autoResetEventSend.Set();
            }
            catch (NullReferenceException ex)
            {
                // ignore. this could potentially happen if _autoResetEventSend were set to null while we were waiting.
            }
            catch (Exception ex)
            {
                _logger.Error("An error ocurred in the tcp ip driver sent data callback.", ex);
            }
            //}
        }



        /// <summary>
        /// Enqueue a byte array to be sent as soon as all previously queued items have been sent.
        /// </summary>
        /// <param name="data">The byte array to enqueue to send.</param>
        public override void Send(byte[] data)
        {
            // Override the base Send() to detect if there is a connection. The string methods do not need to be overridden since they will end up here.
            if (Connected == false)
            {
                string endPointStr = "";
                try
                {
                    if (_socket != null)
                        endPointStr = ((IPEndPoint)_socket.RemoteEndPoint).Address.ToString();
                }
                catch
                {
                    // no big deal.
                }
                throw new Exception("Data cannot be sent since there is no tcp connection established with (" + _tcpHostName + ") " + endPointStr);
            }

            base.Send(data);
        }
        /// <summary>
        /// Enqueue a byte array to be sent the specified milliseconds after the previously queued item has been sent.
        /// </summary>
        /// <param name="data">The byte array to enqueue to send</param>
        /// <param name="delayMilliseconds">The number of milliseconds to wait to send the byte array after the prior send has occurred.</param>
        public override void Send(byte[] data, int delayMilliseconds)
        {
            // Override the base Send() to detect if there is a connection. The string methods do not need to be overridden since they will end up here.
            if (Connected == false)
            {
                string endPointStr = "";
                try
                {
                    if (_socket != null)
                        endPointStr = ((IPEndPoint)_socket.RemoteEndPoint).Address.ToString();
                }
                catch
                {
                    // no big deal.
                }
                throw new Exception("Data cannot be sent since there is no tcp connection established with (" + _tcpHostName + ") " + endPointStr);
            }

            base.Send(data, delayMilliseconds);
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
            lock (cleanUp_Lock)
            {
                try
                {
                    _logger.Debug("TcpCommunication simulating received " + data.Length + " bytes.", data);

                    ProcessReceivedData(data, data.Length);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occurred while simulating received tcp data.", ex);
                }
            }
        }

        public override string ConnectionDisplayText
        {
            get
            {
                if (_socket != null && _socket.RemoteEndPoint != null)
                    return _socket.RemoteEndPoint.ToString();
                else
                    return _tcpHostName + ":" + _tcpPort;
            }
        }
    }
}
