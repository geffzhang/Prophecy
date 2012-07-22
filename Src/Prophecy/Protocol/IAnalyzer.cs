using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prophecy.Protocol
{
    public interface IAnalyzer
    {
        byte[] Raw { get; set; }

        SearchResult SearchBuffer(List<byte> buffer);

        void Analyze();
    }

    public enum SearchResult
    {
        None,
        Mask,
        Data,
        All,
    }
}
