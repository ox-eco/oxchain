using Org.BouncyCastle.Cms;
using OX.IO;
using OX.Ledger;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class RangeTransaction : Transaction
    {
        public uint MaxIndex;
        public uint MinIndex;
        public override int Size => base.Size + sizeof(uint) + sizeof(uint);
        public override Fixed8 SystemFee => AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();

        public RangeTransaction()
            : base(TransactionType.RangeTransaction)
        {
            this.MaxIndex = 0x00;
            this.MinIndex = 0x00;
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            MaxIndex = reader.ReadUInt32();
            MinIndex = reader.ReadUInt32();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(MaxIndex);
            writer.Write(MinIndex);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (MaxIndex > 0)
            {
                if (MaxIndex <= snapshot.Height) return false;
            }
            if (MinIndex > 0)
            {
                if (MinIndex > snapshot.Height + 1) return false;
            }
            return base.Verify(snapshot, mempool);
        }
    }
}
