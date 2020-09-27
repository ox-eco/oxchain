using System;
using System.IO;
using System.Collections.Generic;
using OX.Ledger;
using OX.Persistence;
using OX.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class ContractTransaction : Transaction
    {
        public override Fixed8 SystemFee => Fixed8.Satoshi * 10_000_000 * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage <= TransactionAttributeUsage.Tip10 && m.Data.GetVarSize() > 8).Count();
        public ContractTransaction()
            : base(TransactionType.ContractTransaction)
        {
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
        }
       
    }
}
