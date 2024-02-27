using OX.IO;
using System.Collections.Generic;
using System.IO;
using OX.Persistence;

namespace OX.Network.P2P.Payloads
{
    public abstract class BizTransaction : Transaction
    {
        public UInt160 BizScriptHash;
        public BizTransactionStatus BizTxState { get; protected set; }
        //public override Fixed8 NetworkFee => Fixed8.Zero;
        public override int Size => base.Size + BizScriptHash.Size + sizeof(BizTransactionStatus);

        public BizTransaction(TransactionType type)
            : base(type)
        {
        }

        protected abstract void DeserializeBizData(BinaryReader reader);
        protected abstract void SerializeBizData(BinaryWriter writer);
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            BizScriptHash = reader.ReadSerializable<UInt160>();
            BizTxState = (BizTransactionStatus)reader.ReadByte();
            DeserializeBizData(reader);
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(BizScriptHash);
            writer.Write((byte)BizTxState);
            SerializeBizData(writer);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (BizTxState != BizTransactionStatus.OnChain) return false;
            return base.Verify(snapshot, mempool);
        }
    }
}
