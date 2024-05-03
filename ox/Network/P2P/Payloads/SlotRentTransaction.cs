using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class SlotRentTransaction : Transaction
    {
        public UInt160 ScriptHash;
        public uint RentDuration;
        public Fixed8 AskFee;
        public byte[] Scripts;
        public byte[] Mark;

        public override int Size => base.Size + ScriptHash.Size + sizeof(uint) + AskFee.Size + Scripts.GetVarSize() + Mark.GetVarSize();
        public override Fixed8 SystemFee => Fixed8.One * RentDuration;

        public SlotRentTransaction()
          : base(TransactionType.SlotRentTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            AskFee = Fixed8.Zero;
            Scripts = new byte[] { 0x00 };
            Mark = new byte[] { 0x00 };
        }
        public SlotRentTransaction(UInt160 scriptHash)
            : this()
        {
            ScriptHash = scriptHash;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return this.ScriptHash;
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            ScriptHash = reader.ReadSerializable<UInt160>();
            RentDuration = reader.ReadUInt32();
            AskFee = reader.ReadSerializable<Fixed8>();
            Scripts = reader.ReadVarBytes();
            Mark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write(RentDuration);
            writer.Write(AskFee);
            writer.WriteVarBytes(Scripts);
            writer.WriteVarBytes(Mark);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["scripthash"] = ScriptHash.ToAddress();
            json["detainduration"] = RentDuration.ToString();
            json["flag"] = AskFee.ToString();
            json["scripts"] = Scripts.ToHexString();
            json["mark"] = Mark.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.AskFee > Fixed8.Zero)
            {
                if (this.AskFee < Fixed8.OXU) return false;
                if (this.AskFee > Fixed8.One * 10) return false;
            }
            if (this.RentDuration < 100) return false;
            return base.Verify(snapshot, mempool);
        }
    }
}
