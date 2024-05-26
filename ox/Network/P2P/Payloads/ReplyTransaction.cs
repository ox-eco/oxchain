using OX.IO;
using System;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class ReplyTransaction : CenterTransaction
    {
        public UInt160 To;
        public byte EdgeVersion;
        public byte DataType;
        public byte[] Data;

        public override int Size => base.Size + To.Size + sizeof(byte) + sizeof(byte) + Data.GetVarSize();

        public ReplyTransaction()
            : base(TransactionType.ReplyTransaction)
        {
            this.BizTxState = BizTransactionStatus.OnChain;
            this.To = UInt160.Zero;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            //this.Witnesses = new Witness[0];
        }

        protected override void DeserializeCenterData(BinaryReader reader)
        {
            To = reader.ReadSerializable<UInt160>();
            EdgeVersion = reader.ReadByte();
            DataType = reader.ReadByte();
            Data = reader.ReadVarBytes();
        }
        protected override void SerializeCenterData(BinaryWriter writer)
        {
            writer.Write(To);
            writer.Write(EdgeVersion);
            writer.Write(DataType);
            writer.WriteVarBytes(Data);
        }
        public bool GetDataModel<T>(UInt160[] bizScriptHashs, byte dataType, out T model) where T : ISerializable, new()
        {
            if (bizScriptHashs.IsNullOrEmpty() || !bizScriptHashs.Contains(this.BizScriptHash) || this.DataType != dataType)
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
