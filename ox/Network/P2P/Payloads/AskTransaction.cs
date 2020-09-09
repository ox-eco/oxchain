using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.SmartContract;

namespace OX.Network.P2P.Payloads
{
    public class AskTransaction : EdgeTransaction
    {
        public uint MaxIndex;
        public uint MinIndex;
        public override int Size => base.Size + sizeof(uint) + sizeof(uint);
        public AskTransaction()
            : base(TransactionType.AskTransaction)
        {
            this.BizTxState = BizTransactionStatus.OnChain;
            this.MaxIndex = 0x00;
            this.MinIndex = 0x00;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }

        protected override void DeserializeEdgeData(BinaryReader reader)
        {
            MaxIndex = reader.ReadUInt32();
            MinIndex = reader.ReadUInt32();
        }
        protected override void SerializeEdgeData(BinaryWriter writer)
        {
            writer.Write(MaxIndex);
            writer.Write(MinIndex);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(snapshot, mempool))
                return false;
            if (MaxIndex > 0)
            {
                if (MaxIndex <= snapshot.Height) return false;
            }
            if (MinIndex > 0)
            {
                if (MinIndex > snapshot.Height + 1) return false;
            }
            return true;
        }

    }
}
