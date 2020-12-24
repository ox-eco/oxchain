using OX.Persistence;
using System.Collections.Generic;
using System.IO;
using OX.Ledger;
using System.Linq;

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
            if (!Blockchain.Singleton.VerifyBizValidator(this.BizScriptHash, out Fixed8 balance, out Fixed8 askFee)) return false;
            if (askFee == Fixed8.Zero) return true;
            var outputs = this.Outputs.Where(m => m.AssetId == Blockchain.OXC && m.ScriptHash.Equals(this.BizScriptHash));
            if (outputs.IsNullOrEmpty()) return false;
            return outputs.Sum(m => m.Value) >= askFee;
        }

    }
}
