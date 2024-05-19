using OX.Cryptography;
using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class SlotMark : ISerializable
    {
        public byte[] EnTitle;
        public byte[] EnMark;
        public byte[] CnTitle;
        public byte[] CnMark;

        public virtual int Size => EnTitle.GetVarSize() + EnMark.GetVarSize() + CnTitle.GetVarSize() + CnMark.GetVarSize();
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(EnTitle);
            writer.WriteVarBytes(EnMark);
            writer.WriteVarBytes(CnTitle);
            writer.WriteVarBytes(CnMark);
        }
        public void Deserialize(BinaryReader reader)
        {
            EnTitle = reader.ReadVarBytes();
            EnMark = reader.ReadVarBytes();
            CnTitle = reader.ReadVarBytes();
            CnMark = reader.ReadVarBytes();
        }

    }
    public class SlotRentTransaction : Transaction
    {
        public UInt160 ScriptHash;
        public SlotStatus SlotState;
        public uint SlotRentDuration;
        public Fixed8 AskFee;
        public byte[] SlotMark;

        public override int Size => base.Size + ScriptHash.Size + sizeof(SlotStatus) + sizeof(uint) + AskFee.Size + SlotMark.GetVarSize();
        public override Fixed8 SystemFee
        {
            get
            {
                switch (SlotState)
                {
                    case SlotStatus.Freeze:
                        return Fixed8.One * SlotRentDuration;
                    default:
                        return Fixed8.One * 100;
                }
            }
        }
        public SlotRentTransaction()
          : base(TransactionType.SlotRentTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.AskFee = Fixed8.Zero;
            this.SlotMark = new byte[] { 0x00 };
        }
        public SlotRentTransaction(UInt160 scriptHash, SlotMark mark = default)
            : this()
        {
            this.ScriptHash = scriptHash;
            if (mark.IsNotNull())
                this.SlotMark = mark.ToArray();
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
            SlotState = (SlotStatus)reader.ReadByte();
            SlotRentDuration = reader.ReadUInt32();
            AskFee = reader.ReadSerializable<Fixed8>();
            SlotMark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write((byte)SlotState);
            writer.Write(SlotRentDuration);
            writer.Write(AskFee);
            writer.WriteVarBytes(SlotMark);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["scripthash"] = ScriptHash.ToAddress();
            json["slotstate"] = SlotState.Value();
            json["slotrentduration"] = SlotRentDuration.ToString();
            json["flag"] = AskFee.ToString();
            json["data"] = SlotMark.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.AskFee > Fixed8.Zero)
            {
                if (this.AskFee < Fixed8.OXU) return false;
                if (this.AskFee > Fixed8.One * 10) return false;
            }
            if (this.SlotState == SlotStatus.Freeze && this.SlotRentDuration < 100) return false;
            return base.Verify(snapshot, mempool);
        }
    }
}
