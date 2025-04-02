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

    public class LockAssetTransaction : Transaction
    {
        public ECPoint Recipient;
        public bool IsTimeLock;
        public uint LockExpiration;
        public LockAssetPurpose Purpose;
        public UInt160 LockContract;
        public byte[] Attach;

        public override int Size => base.Size + Recipient.Size + sizeof(bool) + sizeof(uint) + sizeof(LockAssetPurpose) + LockContract.Size + Attach.GetVarSize();
        public override Fixed8 SystemFee => AttributesFee + OutputFee + (Attach.GetVarSize() > 40 ? Fixed8.One : Fixed8.Zero) + purposeFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark1 && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;
        Fixed8 purposeFee
        {
            get
            {
                switch (Purpose)
                {
                    case LockAssetPurpose.BlockBonusVote: return Fixed8.One * 1000;
                    case LockAssetPurpose.DaoVote: return Fixed8.Zero;
                    case LockAssetPurpose.SlotOffVote: return Fixed8.One * 10;
                    default: return Fixed8.Zero;
                }
            }
        }
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
            this.Purpose = LockAssetPurpose.Common;
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
            Purpose = (LockAssetPurpose)reader.ReadByte();
            LockContract = reader.ReadSerializable<UInt160>();
            Attach = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Recipient);
            writer.Write(IsTimeLock);
            writer.Write(LockExpiration);
            writer.Write((byte)Purpose);
            writer.Write(LockContract);
            writer.WriteVarBytes(Attach);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["recipient"] = Recipient.ToString();
            json["purpose"] = Purpose.ToString();
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
                sb.EmitPush(this.Purpose);
                sb.EmitPush(this.LockExpiration);
                sb.EmitPush(this.IsTimeLock);
                sb.EmitAppCall(this.LockContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }
        public bool TryGetLockVote(out ILockVote lockVote)
        {
            lockVote = default;
            if (this.IsTimeLock) return false;
            if (Attach.IsNullOrEmpty()) return false;
            if (Purpose == LockAssetPurpose.Common) return false;
            try
            {
                if (Purpose == LockAssetPurpose.BlockBonusVote)
                {
                    lockVote = Attach.AsSerializable<BlockBonusSetting>();
                }
                else if (Purpose == LockAssetPurpose.SlotOffVote)
                {
                    lockVote = Attach.AsSerializable<SlotOffVote>();
                }
                else if (Purpose == LockAssetPurpose.DaoVote)
                {
                    lockVote = Attach.AsSerializable<DaoVote>();
                }
                return true;
            }
            catch
            {
                lockVote = default;
                return false;
            }
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.LockContract != Blockchain.LockAssetContractScriptHash) return false;
            if (this.Purpose == LockAssetPurpose.Common || this.Purpose == LockAssetPurpose.DaoVote)
                if (this.Outputs.Length > 2) return false;
            var contract = GetContract();
            if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash)).IsNull()) return false;
            if (Purpose != LockAssetPurpose.Common)
            {
                if (!TryGetLockVote(out ILockVote lockVote)) return false;
                if (lockVote.Index <= Blockchain.Singleton.HeaderHeight) return false;
                if (this.LockExpiration < lockVote.Index) return false;

                if (Purpose == LockAssetPurpose.BlockBonusVote)
                {
                    var setting = lockVote as BlockBonusSetting;
                    if (setting.Index % Blockchain.DecrementInterval > 0) return false;
                    var rem = Blockchain.Singleton.HeaderHeight % Blockchain.DecrementInterval;
                    var h = Blockchain.Singleton.HeaderHeight - rem + Blockchain.DecrementInterval;
                    if (h != setting.Index) return false;
                    if (setting.Index > (Blockchain.GenerationBonusAmount.Length - 1) * Blockchain.DecrementInterval) return false;
                    if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(Blockchain.OXS)).IsNull()) return false;

                }
                else if (Purpose == LockAssetPurpose.SlotOffVote)
                {
                    if (lockVote.Index % 10000 > 0) return false;
                    if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(Blockchain.OXS)).IsNull()) return false;
                }
                else if (Purpose == LockAssetPurpose.DaoVote)
                {
                    var daoVote = lockVote as DaoVote;
                    if (lockVote.Index % 10000 > 0) return false;
                    if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(daoVote.AssetId)).IsNull()) return false;
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
                    var currentIssued = mempool.OfType<LockAssetTransaction>().Where(p => p != this && p.IsIssue).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
                    if (Fixed8.MaxValue - asset.Available < currentIssued) return false;
                    Fixed8 quantity_issued = asset.Available + currentIssued;
                    if (asset.Amount - quantity_issued < -r.Amount) return false;
                }
            }
            return true;
            #endregion
        }

    }

}
