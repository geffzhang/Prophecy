using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Prophecy.Protocol
{
    public abstract class AnalyzeResult<T> : IAnalyzer where T : new()
    {
        private Timer timer;
        private T data;
        private bool valid;
        private int passMiliSecond;
        private int timeOut;
        //private AnalyzeResult<T>.GetNewDataHandler WB3dVhkAL;

        private byte[] raw;

        public T Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        public bool Valid
        {
            get
            {
                return valid;
            }
            set
            {
                valid = value;
            }
        }

        public int PassMiliSecond
        {
            get
            {
                return passMiliSecond;
            }

            set
            {
                this.passMiliSecond = value;
            }
        }

        public int TimeOut
        {
            get
            {
                return this.timeOut;
            }
            set
            {
                this.timeOut = value;
            }
        }

        public byte[] Raw
        {
            get
            {
                return raw;
            }
            set
            {
                raw = value;
            }
        }

        public event AnalyzeResult<T>.GetNewDataHandler GetNew;


        //[MethodImpl(MethodImplOptions.NoInlining)]
        //static AnalyzeResult()
        //{
        //  CWcB4Ps6POa2yjnCix.rvVCTraFV();
        //  a9SIhVRXkUq2KqoWmM.\u0038bKuHNrs9R2La();
        //}

        [MethodImpl(MethodImplOptions.NoInlining)]
        public AnalyzeResult()
        {
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static implicit operator T(AnalyzeResult<T> ar)
        //{
        //  return (T) null;
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public override string ToString()
        //{
        //  return (string) null;
        //}

        public abstract SearchResult SearchBuffer(List<byte> buffer);

        public abstract void Analyze();

        public delegate void GetNewDataHandler(AnalyzeResult<T> m);
    }
}
