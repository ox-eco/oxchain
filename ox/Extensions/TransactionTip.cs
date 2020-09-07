using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Ledger;
using System.IO;

namespace OX
{
    public class TransactionTip : ISerializable
    {
        public UInt160 BizScriptHash;
        public byte TipSubType;
        public UInt160 From;
        public UInt160 To;
        public uint MaxIndex;
        public byte[] Data;

        public virtual int Size => BizScriptHash.Size + sizeof(byte) + From.Size + To.Size + sizeof(uint) + Data.GetVarSize();
        public TransactionTip()
        {
            this.BizScriptHash = UInt160.Zero;
            this.From = UInt160.Zero;
            this.To = UInt160.Zero;
            this.MaxIndex = 0;
            this.Data = new byte[] { 0x00 };
        }
        public TransactionTip(UInt160 bizScriptHash, byte subType, UInt160 from) : this()
        {
            this.BizScriptHash = bizScriptHash;
            this.TipSubType = subType;
            this.From = from;
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(BizScriptHash);
            writer.Write(TipSubType);
            writer.Write(From);
            writer.Write(To);
            writer.Write(MaxIndex);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            BizScriptHash = reader.ReadSerializable<UInt160>();
            TipSubType = reader.ReadByte();
            From = reader.ReadSerializable<UInt160>();
            To = reader.ReadSerializable<UInt160>();
            MaxIndex = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }
        public bool GetSubTipModel<T>(byte subType, out T model) where T : ISerializable, new()
        {
            if (this.TipSubType != subType)
            {
                model = default;
                return false;
            }
            try
            {
                model = this.Data.AsSerializable<T>();
                return true;
            }
            catch
            {
                model = default;
                return false;
            }
        }
    }
}
