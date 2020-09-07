using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OX.Network.P2P.Payloads
{
    public class SingleTransactionWrapper
    {
        ContractTransaction tx;
        public UInt160 From { get; private set; }
        public SingleTransactionWrapper(UInt160 from, TransactionOutput output) : base()
        {
            this.From = from;
            tx = new ContractTransaction();
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            tx.Attributes = attributes.ToArray();
            tx.Outputs = new TransactionOutput[] { output };
        }
        public SingleTransactionWrapper(UInt160 from, UInt160 to, UInt256 assetId, Fixed8 amount) : this(from, new TransactionOutput() { AssetId = assetId, ScriptHash = to, Value = amount })
        {
        }
        public ContractTransaction Get()
        {
            return tx;
        }
    }
}
