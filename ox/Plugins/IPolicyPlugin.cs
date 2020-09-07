using OX.Network.P2P.Payloads;
using System.Collections.Generic;

namespace OX.Plugins
{
    public interface IPolicyPlugin
    {
        bool FilterForMemoryPool(Transaction tx);
        IEnumerable<Transaction> FilterForBlock(IEnumerable<Transaction> transactions);
        int MaxTxPerBlock { get; }
        int MaxLowPriorityTxPerBlock { get; }
    }
}
