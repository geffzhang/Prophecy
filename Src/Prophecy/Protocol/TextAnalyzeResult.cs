using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Prophecy.Protocol
{
    public abstract class TextAnalyzeResult<T> : AnalyzeResult<T> where T : new()
  {
        private string beginOfLine;
        private string endOfLine;
    private Encoding encoding;

    public Encoding Encoding
    {
     
        get
      {
        return encoding;
      }
        set
      {
          this.encoding = value;
      }
    }

    public string BeginOfLine
    {
      get
      {
          return beginOfLine;
      }
      set
      {
          this.beginOfLine = value;
      }
    }

    public string EndOfLine
    {
     get
      {
          return endOfLine;
      }
      set
      {
          endOfLine = value;
      }
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    //static TextAnalyzeResult()
    //{
    //  //CWcB4Ps6POa2yjnCix.rvVCTraFV();
    //  //a9SIhVRXkUq2KqoWmM.\u0038bKuHNrs9R2La();
    //}

    //[MethodImpl(MethodImplOptions.NoInlining)]
    //protected TextAnalyzeResult()
    //{
    //}

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override SearchResult SearchBuffer(List<byte> buffer)
    {
      // ISSUE: reference to a compiler-generated field
      //return \u003CPrivateImplementationDetails\u003E\u007BB46DBC44\u002D980C\u002D4A61\u002DAD34\u002D2127ADAACF0E\u007D.fieldimpl1;
        return SearchResult.All;
    }
    }
}
