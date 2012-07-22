using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prophecy.Communication;

namespace Prophecy.Protocol
{
    public class WyzComm
    {
        public WyzComm(ICommunication comm, IAnalyzerCollection result)
        {

        }

        public ICommunication Comm { get; set; }
        public int ReadBufferSize { get; set; }
        public IAnalyzerCollection Results { get; set; }

        public event WyzComm.RawHandler OnRaw;

        public delegate void RawHandler(byte[] bytes);
    }
}
