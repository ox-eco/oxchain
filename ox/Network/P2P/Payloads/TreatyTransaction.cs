using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OX.SmartContract;
using OX.Cryptography.ECC;

namespace OX.Network.P2P.Payloads
{
    public enum TreatyType : byte
    {
        Secret = 0x00,
        Public = 0x01
    }
    public class TreatyTransaction : Transaction
    {
        public ECPoint PartyA;
        public ECPoint PartyB;
        public TreatyType TreatyType;
        public uint DataFormat;
        public uint TreatyVersion;
        public byte[] Data;

        public override int Size => base.Size + PartyA.Size + PartyB.Size + sizeof(TreatyType) + sizeof(uint) + sizeof(uint) + Data.GetVarSize();

        public TreatyTransaction()
          : base(TransactionType.TreatyTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            Data = new byte[] { 0x00 };
        }
        public TreatyTransaction(ECPoint partya, ECPoint partyb)
            : this()
        {
            PartyA = partya;
            PartyB = partyb;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            List<UInt160> list = new List<UInt160>();
            list.Add(Contract.CreateSignatureRedeemScript(PartyA).ToScriptHash());
            list.Add(Contract.CreateSignatureRedeemScript(PartyB).ToScriptHash());
            return list;
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            PartyA = reader.ReadSerializable<ECPoint>();
            PartyB = reader.ReadSerializable<ECPoint>();
            TreatyType = (TreatyType)reader.ReadByte();
            DataFormat = reader.ReadUInt32();
            TreatyVersion = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(PartyA);
            writer.Write(PartyB);
            writer.Write((byte)TreatyType);
            writer.Write(DataFormat);
            writer.Write(TreatyVersion);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["partya"] = PartyA.EncodePoint(false).ToHexString();
            json["partyb"] = PartyB.EncodePoint(false).ToHexString();
            json["treatytype"] = TreatyType.ToString();
            json["dataformat"] = DataFormat.ToString();
            json["treatyversion"] = TreatyVersion.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
           
            return base.Verify(snapshot, mempool);
        }
    }
}
