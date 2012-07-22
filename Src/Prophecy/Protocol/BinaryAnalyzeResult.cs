using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Prophecy.Protocol
{
    public delegate byte CheckSumHandler(List<byte> buf, int i, int len);

    public abstract class BinaryAnalyzeResult<T> : AnalyzeResult<T> where T : new()
    {
        protected byte[] _mask;
        protected CheckSumHandler checksum;
        private int lenLength;

        protected int LenLength
        {            
            get
            {
                return lenLength;
            }           
            set
            {
                lenLength = value;
            }
        }

        public byte[] Mask
        {
            
            get
            {
                return _mask;
            }
          
            protected set
            {
                _mask = value;
            }
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //static BinaryAnalyzeResult()
        //{
        //    //CWcB4Ps6POa2yjnCix.rvVCTraFV();
        //    //a9SIhVRXkUq2KqoWmM.\u0038bKuHNrs9R2La();
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //protected BinaryAnalyzeResult()
        //{
        //}

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override SearchResult SearchBuffer(List<byte> buffer)
        {
            // ISSUE: reference to a compiler-generated field
            //return \u003CPrivateImplementationDetails\u003E\u007BB46DBC44\u002D980C\u002D4A61\u002DAD34\u002D2127ADAACF0E\u007D.fieldimpl1;
            return SearchResult.All;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static byte XorChecksum(List<byte> buf, int index, int len)
        {
            return (byte)0;
        }
    }
}
