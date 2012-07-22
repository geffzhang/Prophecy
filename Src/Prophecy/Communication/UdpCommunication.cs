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
    public class UdpCommunication : BaseCommunication
    {
        private Socket _socket;
        private IPEndPoint _endPoint;
        private byte[] _readBuffer;

        private string _udpHostName;  // hostname
        private int _udpPort; // udp port

        const int READBUFFERSIZE = 4 * 1024;

        private SocketAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Initializes a new instance of the UdpCommunication class.
        /// The class will not listen for incoming data until Open() is called.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect. Specify "255.255.255.255" for the hostname to use broadcast.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        public UdpCommunication(string hostName, int port)
        {
            _udpHostName = hostName;
            _udpPort = port;
        }


        /// <summary>
        /// Create a UdpCommunication object using an existing Socket.
        /// </summary>
        public UdpCommunication(Socket socket, IPEndPoint endPoint)
        {
            _socket = socket;
            _endPoint = endPoint;

            // Start asynchronous read
            try
            {
                _receiveArgs = new SocketAsyncEventArgs();
                _receiveArgs.UserToken = _socket;
                _receiveArgs.RemoteEndPoint = _endPoint;
                _readBuffer = new byte[READBUFFERSIZE];
                _receiveArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
                _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                if (_socket.ReceiveFromAsync(_receiveArgs) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                    _socket_ReceivedData(_socket, _receiveArgs);
            }
            catch (Exception ex)
            {
                // On failure free up the SocketAsyncEventArgs
                if (_receiveArgs != null)
                {
                    _receiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                    _receiveArgs.Dispose();
                    _receiveArgs = null;
                }

                throw;
            }
        }

        /// <summary>
        /// Start listening on the specified UDP port and indicate what ip address to send data to. Specify "255.255.255.255" for the hostname to broadcast.
        /// </summary>
        /// <param name="hostName">The DSN name of the remote host to which you indend to connect. Specify "255.255.255.255" for the hostname to broadcast.</param>
        /// <param name="port">The port number of the remote host to which you indend to connect.</param>
        /// <exception cref="System.ArgumentNullException">The hostname parameter is nullNothingnullptra null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The port parameter is not between MinPort and MaxPort.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">An error occurred when accessing the socket. use SocketException.ErrorCode to obtain the specific error code. After you have obtained this code, you can refer to the Windows Sockets version 2 API error code documentation in MSDN for a detailed description of the error.</exception>
        /// <remarks>
        /// This makes a synchronous connection attempt to the provided host name and port number and will block until it either connects or fails. The underlying service provider will assign the most appropriate local IP address and port number.
        /// </remarks>
        public void Open(string hostName, int port)
        {
            _udpHostName = hostName;
            _udpPort = port;

            base.Open();
        }

        /// <summary>
        /// Starts listening on the specified UDP port specified in the constructor.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">The hostname parameter is a null reference (Nothing in Visual Basic).</exception>
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
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  %%%%%%%%%%%%%%%%%% UdpCommunication.Open.  Attempting to connect to " + _udpHostName + ":" + _udpPort + ".");

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // interNetwork is IPv4

                // Get the end point to send data to.
                IPAddress ipAddress;
                if (System.Net.IPAddress.TryParse(_udpHostName, out ipAddress) == false)
                    ipAddress = Dns.GetHostEntry(_udpHostName).AddressList[0];
                _endPoint = new IPEndPoint(ipAddress, _udpPort);

                
                IPEndPoint bindingEndPoint = null;
                if (IPAddress.Broadcast.Equals(_endPoint.Address))
                {
                    string localHostName = System.Net.Dns.GetHostName();
                    IPAddress[] ipAddresses = Dns.GetHostEntry(localHostName).AddressList;
                    foreach (IPAddress ip in ipAddresses)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            bindingEndPoint = new IPEndPoint(ip, _udpPort); // This needs to be a local ip address, and the port to listen on.
                            break;
                        }
                    }
                    if (bindingEndPoint == null)
                        throw new Exception("There is no outside ip address for this machine.");
                    _socket.EnableBroadcast = true;
                }
                else
                {
                    //IPEndPoint bindingEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _udpPort);
                    bindingEndPoint = new IPEndPoint(IPAddress.Any, _udpPort); // This needs to be a local ip address, and the port to listen on.
                }
                _socket.Bind(bindingEndPoint);


                // Start listening
                _receiveArgs = new SocketAsyncEventArgs();
                try
                {
                    _receiveArgs.UserToken = _socket;
                    _receiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // it doesn't appear to matter what address or port you set this to it just needs to be set to something.
                    _readBuffer = new byte[READBUFFERSIZE];
                    _receiveArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
                    _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                    if (_socket.ReceiveFromAsync(_receiveArgs) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                        _socket_ReceivedData(_socket, _receiveArgs);
                }
                catch
                {
                    // On failure free up the SocketAsyncEventArgs
                    if (_receiveArgs != null)
                    {
                        _receiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                        _receiveArgs.Dispose();
                        _receiveArgs = null;
                    }

                    throw;
                }
            }
        }

        private void _socket_ReceivedData(object sender, SocketAsyncEventArgs e)
        {
            lock (cleanUp_Lock)
            {
                try
                {
                    if (_socket != null)
                    {
                        if (e.BytesTransferred > 0)
                        {
                            _logger.Debug("UdpCommunication received " + e.BytesTransferred + " bytes.", e.Buffer, e.Offset, e.BytesTransferred);
                            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  UdpCommunication received " + e.BytesTransferred + " bytes. hex: " + base.ByteArrayToHexString(e.Buffer, e.Offset, e.BytesTransferred, " "));

                            // Copy the data to a new buffer that will be passed to ProcessReceivedData().
                            byte[] buffer = new byte[e.BytesTransferred];
                            Array.Copy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                            // Process the received data.
                            ProcessReceivedData(buffer, buffer.Length);
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
                        _logger.Error("An error ocurred in the udp ip driver read callback.", ex);
                }
                finally
                {
                    if (_socket != null && e.BytesTransferred > 0) // since we now lock() this section, the stream should always be there.
                    {
                        try
                        {
                            //Prepare to receive more data
                            Socket socket = (Socket)e.UserToken;
                            if (socket.ReceiveFromAsync(e) == false)  // Returns true if the I/O operation is pending. Returns false if the I/O operation completed synchronously and the SocketAsyncEventArgs.Completed event will not be raised.
                                _socket_ReceivedData(sender, e); // I guess this can happen, but it has the potential for a stack overflow error due to recursion if every call is synchronous... but I have never seen a synchronous call here.
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("An error ocurred in the udp ip driver read callback when attempting a Begin read on the socket.", ex);
                        }
                        
                    }
                }
            }
        }

        /// <summary>
        /// UDP is a connection-less protocol and is therefore never connected so this will always return false.
        /// </summary>
        public override bool Connected
        {
            get
            {
                return false;
            }
        }


        /// <summary>
        /// Releases all resources used by the UdpCommunication.
        /// </summary>
        public override void Dispose()
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
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {
                    }

                    if (_receiveArgs != null)
                    {
                        try
                        {
                            _receiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(_socket_ReceivedData);
                            _receiveArgs.Dispose();
                            _receiveArgs = null;
                        }
                        catch
                        {
                        }
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
            try
            {
                lock (_sendRawLockObject)
                {
                    _logger.Debug("UdpCommunication sending " + data.Length + " bytes.", data);

                    _socket.SendTo(data, _endPoint);

                    //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + "  UdpCommunication finished sending " + data.Length + " bytes.");
                }
            }
            catch (SocketException ex)
            {
                // The connection dropped so the data could not be written.
                OnConnectionLost();
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while sending command: " + data, ex);
            }
        }





        /// <summary>
        /// Simulate data being received from the udp connection. The data will be processed as if it were actually received as incoming data from the udp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(string data)
        {
            SimulateReceivedData(CurrentEncoding.GetBytes(data));
        }

        /// <summary>
        /// Simulate data being received from the udp connection. The data will be processed as if it were actually received as incoming data from the udp connection. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(byte[] data)
        {
            lock (cleanUp_Lock)
            {
                try
                {
                    _logger.Debug("UdpCommunication simulating received " + data.Length + " bytes.", data);

                    ProcessReceivedData(data, data.Length);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occurred while simulating received udp data.", ex);
                }
            }
        }


        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override bool ConnectionMonitorEnabled
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override byte[] ConnectionMonitorTestBytes
        {
            get
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
            set
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override string ConnectionMonitorTestRequest
        {
            get
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
            set
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override int ConnectionMonitorTimeout
        {
            get
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
            set
            {
                throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
            }
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override void StartConnectionMonitor()
        {
            throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override void StartConnectionMonitorSync()
        {
            throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
        }
        /// <summary>
        /// Connection Monitoring is not supported with UDP so this member will always throw an exception.
        /// </summary>
        public override void StopConnectionMonitor()
        {
            throw new NotSupportedException("Connection Monitoring is not supported by the UDP protocol because it is connectionless protocol and is therefore never connected.");
        }

        public override string ConnectionDisplayText
        {
            get
            {
                if (_endPoint != null)
                    return _endPoint.ToString();
                else
                    return _udpHostName + ":" + _udpPort;
            }
        }
    }
}
