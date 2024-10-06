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
    public class DeadlineTransaction : Transaction
    {
        public uint UTCLatest;
        public uint UTCEarliest;
        public override int Size => base.Size + sizeof(uint) + sizeof(uint);
        public override Fixed8 SystemFee => AttributesFee + OutputFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark1 && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;
        public DeadlineTransaction()
            : base(TransactionType.DeadlineTransaction)
        {
            this.UTCLatest = 0x00;
            this.UTCEarliest = 0x00;
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            UTCLatest = reader.ReadUInt32();
            UTCEarliest = reader.ReadUInt32();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(UTCLatest);
            writer.Write(UTCEarliest);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            var ts = System.DateTime.UtcNow.ToTimestamp();
            if (UTCLatest > 0)
            {
                if (UTCLatest <=ts) return false;
            }
            if (UTCEarliest > 0)
            {
                if (UTCEarliest > ts + 1) return false;
            }
            return base.Verify(snapshot, mempool);
        }
    }
}
