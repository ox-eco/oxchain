using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class DetainTransaction : Transaction
    {
        public UInt160 ScriptHash;
        public DetainStatus DetainState;
        public uint DetainDuration;
        public uint AskFee;
        public byte[] Data;

        public override int Size => base.Size + ScriptHash.Size + sizeof(DetainStatus) + sizeof(uint) + sizeof(uint) + Data.GetVarSize();
        public override Fixed8 SystemFee
        {
            get
            {
                switch (DetainState)
                {
                    case DetainStatus.Freeze:
                        return Fixed8.FromDecimal((decimal)DetainDuration);
                    default:
                        return Fixed8.FromDecimal(100);
                }
            }
        }
        public DetainTransaction()
          : base(TransactionType.DetainTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            AskFee = 0;
            Data = new byte[] { 0x00 };
        }
        public DetainTransaction(UInt160 scriptHash)
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
            DetainState = (DetainStatus)reader.ReadByte();
            DetainDuration = reader.ReadUInt32();
            AskFee = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write((byte)DetainState);
            writer.Write(DetainDuration);
            writer.Write(AskFee);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["scripthash"] = ScriptHash.ToAddress();
            json["detaintype"] = DetainState.Value();
            json["detainduration"] = DetainDuration.ToString();
            json["flag"] = AskFee.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.AskFee > 1000) return false;
            if (this.DetainState == DetainStatus.Freeze && this.DetainDuration < 100) return false;
            return base.Verify(snapshot, mempool);
        }
    }
}
