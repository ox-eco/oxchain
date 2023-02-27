using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using OX.Cryptography.ECC;
using OX.SmartContract;
using OX.Ledger;
using OX.Cryptography;
using OX.VM;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace OX.Network.P2P.Payloads
{
    public enum SideType : byte
    {
        AssetID = 0x01,
        ScriptHash = 0x02,
        TimeStamp = 0x03,
        BlockStamp = 0x04,
        Hash = 0x05,
        PublicKey = 0x06
    }
    public class SideTransaction : Transaction
    {
        public ECPoint Recipient;
        public SideType SideType;
        public byte[] Data;
        public byte Flag;
        public UInt160 MergeContract;
        public byte[] Attach;

        public override int Size => base.Size + Recipient.Size + sizeof(SideType) + Data.GetVarSize() + sizeof(byte) + MergeContract.Size + Attach.GetVarSize();
        public override Fixed8 SystemFee => Attach.Length > 0 ? Fixed8.One : Fixed8.Zero + AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.RelatedScriptHash && m.Data.GetVarSize() > 8).Count();

        public SideTransaction()
          : base(TransactionType.SideTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Attach = new byte[0];
        }


        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Recipient = reader.ReadSerializable<ECPoint>();
            SideType = (SideType)reader.ReadByte();
            Data = reader.ReadVarBytes();
            Flag = reader.ReadByte();
            MergeContract = reader.ReadSerializable<UInt160>();
            Attach = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Recipient);
            writer.Write((byte)SideType);
            writer.WriteVarBytes(Data);
            writer.Write(Flag);
            writer.Write(MergeContract);
            writer.WriteVarBytes(Attach);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["recipient"] = Recipient.ToString();
            json["sidetype"] = SideType.ToString();
            json["data"] = Data.ToHexString();
            json["flag"] = Flag.ToString();
            json["lockcontract"] = MergeContract.ToString();
            json["attach"] = Attach.ToHexString();
            return json;
        }
        public Contract GetContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(this.Recipient);
                sb.EmitPush(this.Flag);
                sb.EmitPush(this.Data);
                sb.EmitPush((byte)this.SideType);
                sb.EmitAppCall(this.MergeContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Data.IsNullOrEmpty()) return false;
            //if (this.Outputs.Length > 2) return false;
            if (!VerifyData()) return false;
            if (SideType == SideType.AssetID)
            {
                var assetId = Data.AsSerializable<UInt256>();
                if (snapshot.Assets.TryGet(assetId).IsNull()) return false;
            }
            var contract = GetContract();
            if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash)).IsNull()) return false;
            return base.Verify(snapshot, mempool);
        }
        bool VerifyData()
        {
            switch (SideType)
            {
                case SideType.AssetID:
                    try
                    {
                        Data.AsSerializable<UInt256>();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case SideType.ScriptHash:
                    try
                    {
                        Data.AsSerializable<UInt160>();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case SideType.TimeStamp:
                    try
                    {
                        BitConverter.ToUInt32(Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case SideType.BlockStamp:
                    try
                    {
                        BitConverter.ToUInt32(Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case SideType.Hash:
                    try
                    {
                        Data.AsSerializable<UInt256>();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case SideType.PublicKey:
                    try
                    {
                        Data.AsSerializable<ECPoint>();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }
            return false;
        }
    }

}
