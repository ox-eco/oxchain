﻿using OX.IO;
using OX.Ledger;
using System;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class ContractTransaction : Transaction
    {
        public override Fixed8 SystemFee => AttributesFee+OutputFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;
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
