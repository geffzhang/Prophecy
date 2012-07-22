using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Prophecy.Protocol
{
    public interface IAnalyzerCollection 
        : IEnumerable<IAnalyzer>, IEnumerable
    {
    }
}
