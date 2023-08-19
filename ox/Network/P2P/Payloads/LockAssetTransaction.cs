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
    public class BlockBonusSetting : ISerializable
    {
        public uint Index;
        public byte NumPerBlock;
        public virtual int Size => sizeof(uint) + sizeof(byte);
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(NumPerBlock);
        }
        public void Deserialize(BinaryReader reader)
        {
            Index = reader.ReadUInt32();
            NumPerBlock = reader.ReadByte();
        }
        public override bool Equals(object obj)
        {
            if (obj is BlockBonusSetting bbs)
            {
                return bbs.Index == this.Index && bbs.NumPerBlock == this.NumPerBlock;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return (Index + NumPerBlock).GetHashCode();
        }
    }
    public class LockAssetTransaction : Transaction
    {
        public ECPoint Recipient;
        public bool IsTimeLock;
        public uint LockExpiration;
        public byte Flag;
        public UInt160 LockContract;
        public byte[] Attach;

        public override int Size => base.Size + Recipient.Size + sizeof(bool) + sizeof(uint) + sizeof(byte) + LockContract.Size + Attach.GetVarSize();
        public override Fixed8 SystemFee => AttributesFee +OutputFee+ (Attach.GetVarSize() > 8 ? Fixed8.One : Fixed8.Zero) + (Flag == byte.MaxValue ? Fixed8.One * 1000 : Fixed8.Zero);
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;
        #region append for Issue
        public bool IsIssue
        {
            get
            {
                if (this.Inputs.IsNullOrEmpty() && this.Outputs.IsNotNullAndEmpty()) return true;
                TransactionResult[] results = GetTransactionResults()?.Where(p => p.Amount < Fixed8.Zero).ToArray();
                if (results.IsNullOrEmpty()) return false;
                return true;
            }
        }

        #endregion
        public LockAssetTransaction()
          : base(TransactionType.LockAssetTransaction)
        {
            this.LockContract = Blockchain.LockAssetContractScriptHash;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Attach = new byte[0];
        }
        #region append for Issue
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            foreach (TransactionResult result in GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
            {
                AssetState asset = snapshot.Assets.TryGet(result.AssetId);
                if (asset == null) throw new InvalidOperationException();
                hashes.Add(asset.Issuer);
            }
            return hashes.OrderBy(p => p).ToArray();
        }
        #endregion
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Recipient = reader.ReadSerializable<ECPoint>();
            IsTimeLock = reader.ReadBoolean();
            LockExpiration = reader.ReadUInt32();
            Flag = reader.ReadByte();
            LockContract = reader.ReadSerializable<UInt160>();
            Attach = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Recipient);
            writer.Write(IsTimeLock);
            writer.Write(LockExpiration);
            writer.Write(Flag);
            writer.Write(LockContract);
            writer.WriteVarBytes(Attach);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["recipient"] = Recipient.ToString();
            json["istimelock"] = IsTimeLock.ToString();
            json["lockexpiration"] = LockExpiration.ToString();
            json["lockcontract"] = LockContract.ToString();
            return json;
        }
        public Contract GetContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(this.Recipient);
                sb.EmitPush(this.Flag);
                sb.EmitPush(this.LockExpiration);
                sb.EmitPush(this.IsTimeLock);
                sb.EmitAppCall(this.LockContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Outputs.Length > 2) return false;
            var contract = GetContract();
            if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash)).IsNull()) return false;
            if (Flag == byte.MaxValue)
            {
                if (this.IsTimeLock) return false;
                if (Attach.IsNullOrEmpty()) return false;
                try
                {
                    var setting = Attach.AsSerializable<BlockBonusSetting>();
                    if (setting.Index <= Blockchain.Singleton.HeaderHeight) return false;
                    if (this.LockExpiration < setting.Index) return false;
                    if (setting.Index % Blockchain.DecrementInterval > 0) return false;
                    var rem = Blockchain.Singleton.HeaderHeight % Blockchain.DecrementInterval;
                    var h = Blockchain.Singleton.HeaderHeight - rem + Blockchain.DecrementInterval;
                    if (h != setting.Index) return false;
                    if (setting.Index > (Blockchain.GenerationBonusAmount.Length - 1) * Blockchain.DecrementInterval) return false;
                    if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(Blockchain.OXS)).IsNull()) return false;
                }
                catch
                {
                    return false;
                }
            }
            #region append for Issue
            if (!base.Verify(snapshot, mempool)) return false;
            if (IsIssue)
            {
                TransactionResult[] results = GetTransactionResults()?.Where(p => p.Amount < Fixed8.Zero).ToArray();
                if (results == null) return false;
                foreach (TransactionResult r in results)
                {
                    AssetState asset = snapshot.Assets.TryGet(r.AssetId);
                    if (asset == null) return false;
                    if (asset.Amount < Fixed8.Zero) continue;
                    Fixed8 quantity_issued = asset.Available + mempool.OfType<LockAssetTransaction>().Where(p => p != this && p.IsIssue).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
                    if (asset.Amount - quantity_issued < -r.Amount) return false;
                }
            }
            return true;
            #endregion
        }
        public bool ValidBlockBonusVote(out BlockBonusSetting setting, out Fixed8 amount)
        {
            setting = default;
            amount = Fixed8.Zero;
            if (this.Flag == byte.MaxValue)
            {
                var contract = GetContract();
                setting = this.Attach.AsSerializable<BlockBonusSetting>();
                amount = this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(Blockchain.OXS)).Value;
                return true;
            }
            return false;
        }
    }

}
