using OX.IO;
using OX.Ledger;
using System;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class ContractTransaction : Transaction
    {
        public override Fixed8 SystemFee => TransferFee + AttributesFee;
        public Fixed8 TransferFee => Fixed8.OXU * (Math.Min(Blockchain.Singleton.Height / 2_000_000, 10));
        public Fixed8 AttributesFee => Fixed8.OXU * 10* this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage <= TransactionAttributeUsage.Tip10 && m.Data.GetVarSize() > 8).Count();

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
