using OX.Cryptography;
using OX.IO;
using OX.Ledger;
using OX.Persistence;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public abstract class CenterTransaction : BizTransaction
    {
        public uint BizNo;
        public override int Size => base.Size + sizeof(uint);

        public CenterTransaction(TransactionType type)
            : base(type)
        {
        }
        protected abstract void DeserializeCenterData(BinaryReader reader);
        protected abstract void SerializeCenterData(BinaryWriter writer);
        protected override void DeserializeBizData(BinaryReader reader)
        {
            BizNo = reader.ReadUInt32();
            DeserializeCenterData(reader);
        }
        protected override void SerializeBizData(BinaryWriter writer)
        {
            writer.Write(BizNo);
            SerializeCenterData(writer);
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }

        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return this.BizScriptHash;
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(snapshot, mempool))
                return false;
            return Blockchain.Singleton.VerifyBizValidator(snapshot, this.BizScriptHash, out Fixed8 balance, out Fixed8 askFee);
        }
    }
}
