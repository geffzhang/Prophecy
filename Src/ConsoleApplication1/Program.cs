using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prophecy.Communication;
using ProtoBuf;
using Prophecy;
using System.IO;
using ProtoBuf.Data;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            // While this example uses the SerialCommunication class, any of the communication classes could be used... they all implement ICommunication.
            // For simplicity we will use 8N1 and not ask on the form.
            ICommunication _comm = new SerialCommunication("COM8", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            _comm.Logger = new Prophecy.Logger.TraceLogger(Prophecy.Logger.LoggerVerbosity.Normal); // assign the built-in TraceLogger just as an example. You could create your own logger class.
            //_comm.Delimiter = "\r"; // change the delimiter since it defaults to "\r\n".
            //_comm.CurrentEncoding = System.Text.ASCIIEncoding.ASCII;  // this is unnecessary since it defaults to ASCII
            //_comm.ReceivedDelimitedString += new EventHandler<ReceivedDelimitedStringEventArgs>(_comm_ReceivedDelimitedString); // Since our pretend protocol uses \r as the delimiter let's make life easy and subscribe to the ReceivedDelimitedString event. We could also subscribe to ReceivedString and ReceivedBytes.
            //_comm.IncludeDelimiterInRawResponse = false; // this is unnecessary since it defaults to false
            //_comm.ReadBufferEnabled = false; // this is unnecessary since it defaults to false and it since we are using the ReceivedDelimitedString event there would be no need to use a buffer.

            _comm.Delimiter = null; // set to null to disable the ReceivedDelimitedString
            _comm.CurrentEncoding = null; // do not decode strings
            _comm.ReceivedBytes += new EventHandler<ReceivedBytesEventArgs>(_serial_DataReceived);
            _comm.ReadBufferEnabled = true; // If the ReadBufferEnabled property is true, you may 
            
            _comm.DefaultSendDelayInterval = 0; // this is unnecessary since it defaults to 0

            // Start connection monitor.
            _comm.ConnectionMonitorTimeout = 60000;
            //_comm.ConnectionMonitorTestRequest = "HELLO\r"; // a pretend message to send when no data has been received for a while (note that serial connections can not detect physical disconnections so we count on this).
            _comm.ConnectionEstablished += new EventHandler<EventArgs>(_comm_ConnectionEstablished);
            _comm.ConnectionLost += new EventHandler<EventArgs>(_comm_ConnectionLost);
            _comm.StartConnectionMonitor(); // if we were not using connection monitoring we could call _comm.Open().
            Console.Read();
        }

        static void  _comm_ConnectionEstablished(object sender, EventArgs e)
        {
            Console.WriteLine( "Status: Connected");
        }

        static void _comm_ConnectionLost(object sender, EventArgs e)
        {
            Console.WriteLine( "Status: Not Connected");
        }

        static void _comm_ReceivedDelimitedString(object sender, ReceivedDelimitedStringEventArgs e)
        {
            // This is where you would process the message (or queue it to be processed later).
            Console.WriteLine(e.RawResponse + "\r\n");          
        }

        static void _serial_DataReceived(object sender, ReceivedBytesEventArgs e)
        {
            // This is where you would process the message (or queue it to be processed later).
            //Console.WriteLine(e.ReceiveBuffer + "\r\n");
            using (MemoryStream ms = new MemoryStream(e.ReceiveBuffer))
            {
                var table = DataSerializer.DeserializeDataTable(ms);

                //Customer customer = Serializer.DeserializeWithLengthPrefix<Customer>(ms,PrefixStyle.Base128);
                //byte[] byteArray = customer.BytesData;
                //string str = Encoding.Unicode.GetString(byteArray);
                //Console.WriteLine(str);
                //Console.WriteLine(customer.CustomerID);
                Console.WriteLine(table.ToString());
            }
        }
    }
}
