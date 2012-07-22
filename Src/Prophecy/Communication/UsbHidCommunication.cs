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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace Prophecy.Communication
{
    public class FoundUsbDeviceEventArgs : EventArgs
    {
        public string Devicepath;
        public bool Cancel;

        public FoundUsbDeviceEventArgs(string devicePath)
        {
            this.Devicepath = devicePath;
            this.Cancel = false;
        }
    }

    /// <summary>
    /// Provides client connections for USB HID Communication devices.
    /// </summary>
    public class UsbHidCommunication : BaseCommunication
    {
        private int _vendorID;
		private int _productID;

        private SafeFileHandle _handle;
		private FileStream _stream;

        private bool _isDisposed;

        /// <summary>
        /// The FoundUsbDevice event is triggered when a device is found with a matching vendor and product id, in which you may conditionally cancel the connection (for example when more than one of the device is found). If the FoundUsbDevice event is not handled then the first matching device will be used.
        /// </summary>
        public event EventHandler<FoundUsbDeviceEventArgs> FoundUsbDevice;

        public bool _useAlternateWriteMethod = false;

        /// <summary>
        /// Initializes a new instance of the UsbHidCommunication class for the specified vendor id and product id.
        /// A connection is not established until Open() or StartMonitoring() is called.
        /// </summary>
        /// <param name="vendorID">The vendor id of the device.</param>
        /// <param name="productID">The product id of the device.</param>
        public UsbHidCommunication(int vendorID, int productID)
        {
            _vendorID = vendorID;
            _productID = productID;
        }


        /// <summary>
        /// Opens a new UsbHidCommunication connection. The FoundUsbDevice event will be triggered when a device is found with a matching vendor and product id, in which you may conditionally cancel the connection (for example when more than one of the device is found). If the FoundUsbDevice event is not handled then the first matching device will be used.
        /// </summary>
        public override void Open()
        {
            // This method is only hear so that the user can see intellisense documentation specific to this class.
            base.Open();
        }

        protected override void open()
        {
            if (_handle == null || _handle.IsClosed == true)
            {
                _isDisposed = false;

                int index = 0;
                bool found = false;
                Guid guid;
                SafeFileHandle handle;

                // get the GUID of the HID class
                Win32UsbHidImports.HidD_GetHidGuid(out guid);

                // get a handle to all devices that are part of the HID class
                // Fun fact:  DIGCF_PRESENT worked on my machine just fine.  I reinstalled Vista, and now it no longer finds the Wiimote with that parameter enabled...
                IntPtr hDevInfo = Win32UsbHidImports.SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, Win32UsbHidImports.DIGCF_DEVICEINTERFACE);// | HIDImports.DIGCF_PRESENT);

                // create a new interface data struct and initialize its size
                Win32UsbHidImports.SP_DEVICE_INTERFACE_DATA diData = new Win32UsbHidImports.SP_DEVICE_INTERFACE_DATA();
                diData.cbSize = Marshal.SizeOf(diData);

                // get a device interface to a single device (enumerate all devices)
                while (Win32UsbHidImports.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, index, ref diData))
                {
                    UInt32 size;

                    // get the buffer size for this device detail instance (returned in the size parameter)
                    Win32UsbHidImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, IntPtr.Zero, 0, out size, IntPtr.Zero);

                    // create a detail struct and set its size
                    Win32UsbHidImports.SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new Win32UsbHidImports.SP_DEVICE_INTERFACE_DETAIL_DATA();

                    // yeah, yeah...well, see, on Win x86, cbSize must be 5 for some reason.  On x64, apparently 8 is what it wants.
                    // someday I should figure this out.  Thanks to Paul Miller on this...
                    diDetail.cbSize = (uint)(IntPtr.Size == 8 ? 8 : 5);

                    // actually get the detail struct
                    if (Win32UsbHidImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, IntPtr.Zero))
                    {
                        Debug.WriteLine(string.Format("{0}: {1} - {2}", index, diDetail.DevicePath, Marshal.GetLastWin32Error()));

                        // open a read/write handle to our device using the DevicePath returned
                        handle = Win32UsbHidImports.CreateFile(diDetail.DevicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, Win32UsbHidImports.EFileAttributes.Overlapped, IntPtr.Zero);

                        // create an attributes struct and initialize the size
                        Win32UsbHidImports.HIDD_ATTRIBUTES attrib = new Win32UsbHidImports.HIDD_ATTRIBUTES();
                        attrib.Size = Marshal.SizeOf(attrib);

                        // get the attributes of the current device
                        if (Win32UsbHidImports.HidD_GetAttributes(handle.DangerousGetHandle(), ref attrib))
                        {
                            // if the vendor and product IDs match up
                            if (attrib.VendorID == _vendorID && attrib.ProductID == _productID)
                            {
                                // Found a matching vendor id and product id.
                                found = true;

                                // Trigger event allowing user to cancel the connection to this device. This is helpful when the multiple of the same device are connected.
                                if (FoundUsbDevice != null)
                                {
                                    FoundUsbDeviceEventArgs args = new FoundUsbDeviceEventArgs(diDetail.DevicePath);
                                    FoundUsbDevice(this, args);
                                    found = !args.Cancel;
                                }

                                if (found)
                                {
                                    _stream = new FileStream(handle, FileAccess.ReadWrite, 1024, true);

                                    _handle = handle;
                                    byte[] buff = new byte[1024];
                                    _stream.BeginRead(buff, 0, 1024, new AsyncCallback(usb_DataReceived), buff);

                                    break;
                                }
                            
                            }
                        }
                        handle.Close();
                    }
                    else
                    {
                        // failed to get the detail struct
                        throw new Exception("SetupDiGetDeviceInterfaceDetail failed on index " + index);
                    }

                    // move to the next device
                    index++;
                }

                // clean up our list
                Win32UsbHidImports.SetupDiDestroyDeviceInfoList(hDevInfo);

                // if we didn't find a Wiimote, throw an exception
                if (!found)
                    throw new Exception("No usb device found in HID device list matching vendor and product id.");
            }
        }

        protected override void OnConnectionAttempt()
        {
            if (_stream == null || _stream.CanRead == false)
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
        /// Gets or sets the USB device vendor ID to find.
        /// </summary>
        public int VendorID
        {
            get { return _vendorID; }
            set { _vendorID = value; }
        }

        /// <summary>
        /// Gets or sets the USB device product ID to find.
        /// </summary>
        public int ProductID
        {
            get { return _productID; }
            set { _productID = value; }
        }

        /// <summary>
        /// Indicates if data should be sent using the stream (false) or using HidD_SetOutputReport (true).
        /// The default id false, which is to send using the stream.
        /// </summary>
        public bool UseAlternateSendMethod
        {
            get { return _useAlternateWriteMethod; }
            set { _useAlternateWriteMethod = value; }
        }

        private readonly object usb_DataReceivedLockObject = new object();
        private void usb_DataReceived(IAsyncResult ar)
        {
            lock (usb_DataReceivedLockObject)
            {
                byte[] buffer = null;
                int byteCount = -1;
                try
                {
                    if (_stream != null)
                    {
                        buffer = (byte[])ar.AsyncState;
                        int bytesRead = _stream.EndRead(ar);
                        if (byteCount > 0)
                        {
                            _logger.Debug("UsbHidCommunication received " + bytesRead + " bytes.", buffer, 0, bytesRead);

                            // Process the received data.
                            ProcessReceivedData(buffer, bytesRead);
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
                    _logger.Error("An error occurred while receiving usb data.", ex);
                }
                finally
                {
                    if (_stream != null && byteCount > 0) // since we now lock() this section, the stream should always be there.
                    {
                        try
                        {
                            // BeginRead() will throw an IOException if the underlying Socket is closed, or there was a failure while reading from the network, or an error occurred when accessing the socket.
                            _stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(usb_DataReceived), buffer);
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
        }


        private readonly object _sendRawLockObject = new object();
        protected override void sendData(byte[] data)
        {
            if (data == null)
            {
                _logger.Error("Data can not be sent via usb because the data value is null.");
                return;
            }

            if (_handle == null || _stream == null)
            {
                _logger.Error("Data can not be sent via usb because there is currently no connection. Data Length: " + data.Length, data, 0, Math.Min(data.Length, 200));
                return;
            }

            try
            {
                lock (_sendRawLockObject)
                {
                    _logger.Debug("UsbHidCommunication sending", data);

                    if (_useAlternateWriteMethod)
                        Win32UsbHidImports.HidD_SetOutputReport(_handle.DangerousGetHandle(), data, (uint)data.Length);
                    else if (_stream != null)
                        _stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("An error occurred while sending usb data: " + base.ByteArrayToHexString(data, 0, data.Length, " ", 200), ex);
            }
        }

        /// <summary>
        /// Indicates if the pc is successfully communicating with the the usb device.
        /// </summary>
        public override bool Connected
        {
            get
            {
                if (base.ConnectionMonitorEnabled)
                    return _handle != null && _handle.IsClosed == false && base.Connected;
                else
                    return _handle != null && _handle.IsClosed == false;
            }
        }

        protected override void OnConnectionLost()
        {
            cleanUp();

            base.OnConnectionLost();
        }

        private void cleanUp()
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_handle != null)
                {
                    _handle.Close();
                    _handle = null;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Releases all resources used by the UsbHidCommunication.
        /// </summary>
        public override void Dispose()
        {
            _isDisposed = true;

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

        /// <summary>
        /// Simulate data being received from the usb port. The data will be processed as if it were actually received as incoming data from the usb ports. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(string data)
        {
            SimulateReceivedData(CurrentEncoding.GetBytes(data));
        }

        /// <summary>
        /// Simulate data being received from the usb port. The data will be processed as if it were actually received as incoming data from the usb ports. This is useful for testing.
        /// </summary>
        /// <param name="data">The data to simulate being received.</param>
        public override void SimulateReceivedData(byte[] data)
        {
            lock (usb_DataReceivedLockObject)
            {
                try
                {
                    _logger.Debug("UsbHidCommunication simulating received " + data.Length + " bytes.", data);

                    ProcessReceivedData(data, data.Length);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occurred while simulating received usb data.", ex);
                }
            }
        }

        public override string ConnectionDisplayText
        {
            get
            {
                return "Vendor ID: " + _vendorID + ", Product ID: " + _productID;
            }
        }
    }

    class Win32UsbHidImports
    {
        // Flags controlling what is included in the device information set built by SetupDiGetClassDevs.
        public const int DIGCF_DEFAULT = 0x00000001; // only valid with DIGCF_DEVICEINTERFACE
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_ALLCLASSES = 0x00000004;
        public const int DIGCF_PROFILE = 0x00000008;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr RESERVED;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public short VendorID;
            public short ProductID;
            public short VersionNumber;
        }

        [DllImport(@"hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void HidD_GetHidGuid(out Guid gHid);

        [DllImport("hid.dll")]
        public static extern Boolean HidD_GetAttributes(IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll")]
        internal extern static bool HidD_SetOutputReport(
            IntPtr HidDeviceObject,
            byte[] lpReportBuffer,
            uint ReportBufferLength);

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPTStr)] string Enumerator,
            IntPtr hwndParent,
            UInt32 Flags
            );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo,
            //ref SP_DEVINFO_DATA devInfo,
            IntPtr devInvo,
            ref Guid interfaceClassGuid,
            Int32 memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
        );

        [DllImport(@"setupapi.dll", SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            UInt32 deviceInterfaceDetailDataSize,
            out UInt32 requiredSize,
            IntPtr deviceInfoData
        );

        [DllImport(@"setupapi.dll", SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            UInt32 deviceInterfaceDetailDataSize,
            out UInt32 requiredSize,
            IntPtr deviceInfoData
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern UInt16 SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] EFileAttributes flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}
