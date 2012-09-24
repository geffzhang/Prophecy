using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Prophecy.Communication;
using Prophecy;
using ProtoBuf;
using System.IO;
using ProtoBuf.Data;

namespace Example
{
    public partial class Form1 : Form
    {
        ICommunication _comm;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // While this example uses the SerialCommunication class, any of the communication classes could be used... they all implement ICommunication.
            // For simplicity we will use 8N1 and not ask on the form.
            _comm = new SerialCommunication(txtSerialPort.Text, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            _comm.Logger = new Prophecy.Logger.TraceLogger(Prophecy.Logger.LoggerVerbosity.Normal); // assign the built-in TraceLogger just as an example. You could create your own logger class.
            //_comm.Delimiter = "\r"; // change the delimiter since it defaults to "\r\n".
            //_comm.CurrentEncoding = System.Text.ASCIIEncoding.ASCII;  // this is unnecessary since it defaults to ASCII
            //_comm.ReceivedDelimitedString += new EventHandler<ReceivedDelimitedStringEventArgs>(_comm_ReceivedDelimitedString); // Since our pretend protocol uses \r as the delimiter let's make life easy and subscribe to the ReceivedDelimitedString event. We could also subscribe to ReceivedString and ReceivedBytes.
            //_comm.IncludeDelimiterInRawResponse = false; // this is unnecessary since it defaults to false
            //_comm.ReadBufferEnabled = false; // this is unnecessary since it defaults to false and it since we are using the ReceivedDelimitedString event there would be no need to use a buffer.
            _comm.Delimiter = null; // set to null to disable the ReceivedDelimitedString
            _comm.CurrentEncoding = null; // do not decode strings
            //_comm.ReceivedBytes += new EventHandler<ReceivedBytesEventArgs>(_serial_DataReceived);
            _comm.ReadBufferEnabled = true; // If the ReadBufferEnabled property is true, you may 

            _comm.DefaultSendDelayInterval = 0; // this is unnecessary since it defaults to 0
            
            _comm.DefaultSendDelayInterval = 0; // this is unnecessary since it defaults to 0

            // Start connection monitor.
            _comm.ConnectionMonitorTimeout = 60000;
            //_comm.ConnectionMonitorTestRequest = "HELLO\r"; // a pretend message to send when no data has been received for a while (note that serial connections can not detect physical disconnections so we count on this).
            _comm.ConnectionEstablished += new EventHandler<EventArgs>(_comm_ConnectionEstablished);
            _comm.ConnectionLost += new EventHandler<EventArgs>(_comm_ConnectionLost);
            _comm.StartConnectionMonitor(); // if we were not using connection monitoring we could call _comm.Open().

            txtSerialPort.Enabled = false;
            btnStart.Enabled = false;
        }

        void _comm_ConnectionEstablished(object sender, EventArgs e)
        {
            this.invokeIfRequired(() => { lblStatus.Text = "Status: Connected"; });
        }

        void _comm_ConnectionLost(object sender, EventArgs e)
        {
            this.invokeIfRequired(() => { lblStatus.Text = "Status: Not Connected"; });
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //_comm.Send(txtSend.Text + "\r"); // our pretend protocol uses \r as the message delimiter.
            //Customer customer = Customer.GetOneCustomer();
            var innerA = TestData.FromMatrix(new[]
            {
                new object[] { "A", "B" },
                new object[] { 1, 2 },
                new object[] { 3, 4 }
            });
            
            var innerB = TestData.FromMatrix(new[]
            { 
                new object[] { "C", "D" },
                new object[] { 5, 6 }, 
                new object[] { 7, 8 } }); 
            
            var table = TestData.FromMatrix(new[] { new object[] { "E", "F" }, new object[] { "A", innerA }, new object[] { "B", innerB } });

            using (MemoryStream ms = new MemoryStream())
            {
                DataSerializer.Serialize(ms, table, new ProtoDataWriterOptions());
                //Serializer.Serialize(ms, customer);
                //Serializer.SerializeWithLengthPrefix<Customer>(ms, customer, PrefixStyle.Base128);
                _comm.Send(ms.ToArray());
                //Console.WriteLine("ProtoBuf Length:{0}", ms.ToArray());
            }

        }

        private void btnSimulateReceivedData_Click(object sender, EventArgs e)
        {
            _comm.SimulateReceivedData(txtSimulateReceivedData.Text + "\r"); // our pretend protocol uses \r as the message delimiter.)
        }

        void _comm_ReceivedDelimitedString(object sender, ReceivedDelimitedStringEventArgs e)
        {
            // This is where you would process the message (or queue it to be processed later).
            this.invokeIfRequired(() =>
            {
                txtReceivedData.AppendText(e.RawResponse + "\r\n");
            });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_comm != null)
            {
                _comm.Dispose();
                _comm = null;
            }
        }


        // this is just a helper function to prevent cross threading issues.
        void invokeIfRequired(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
    }
}
