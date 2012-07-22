/* **********************************************************************************
 * Copyright (c) 2011 John Hughes
 * 
 * Prophecy is licenced under the Microsoft Reciprocal License (Ms-RL).
 *
 * Project Website: http://prophecy.codeplex.com/
 * **********************************************************************************/

using System;
using Prophecy.Logger;

namespace Prophecy.Communication
{
    public interface ICommunication : IDisposable
    {
        event EventHandler CommunicationEnded;
        event EventHandler CommunicationStarted;
        bool Connected { get; }
        event EventHandler<ConnectionAttemptFailedEventArgs> ConnectionAttemptFailed;
        event EventHandler<EventArgs> ConnectionEstablished;
        event EventHandler<EventArgs> ConnectionLost;
        bool ConnectionMonitorEnabled { get; }
        event EventHandler ConnectionMonitorTest;
        byte[] ConnectionMonitorTestBytes { get; set; }
        string ConnectionMonitorTestRequest { get; set; }
        int ConnectionMonitorTimeout { get; set; }
        System.Text.Encoding CurrentEncoding { get; set; }
        int DefaultSendDelayInterval { get; set; }
        string Delimiter { get; set; }
        void Dispose(int timeoutMilliseconds);
        void Flush();
        void Flush(int timeoutMilliseconds);
        bool IncludeDelimiterInRawResponse { get; set; }
        bool IsCommunicating { get; }
        bool IsDisposed { get; }
        ILogger Logger { get; set; }
        void Open();
        void ProcessReceivedData(byte[] data, int byteCount);
        BufferReader ReadBuffer { get; }
        bool ReadBufferEnabled { get; set; }
        event EventHandler ReadBufferOverflow;
        event EventHandler<ReceivedBytesEventArgs> ReceivedBytes;
        event EventHandler<ReceivedDelimitedStringEventArgs> ReceivedDelimitedString;
        event EventHandler<ReceivedStringEventArgs> ReceivedString;
        void ResetByteCountTotals();
        void Send(byte[] data);
        void Send(byte[] data, int delayMilliseconds);
        void Send(string s);
        void Send(string s, int delayMilliseconds);
        void SimulateReceivedData(byte[] data);
        void SimulateReceivedData(string data);
        void StartConnectionMonitor();
        void StartConnectionMonitorSync();
        void StopConnectionMonitor();
        ulong TotalReceivedByteCount { get; }
        ulong TotalSentByteCount { get; }
        string ConnectionDisplayText { get; }
    }
}
