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
        public UInt160 LockContract;

        public override int Size => base.Size + Recipient.Size + sizeof(bool) + sizeof(uint) + LockContract.Size;
        public override Fixed8 SystemFee => AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage <= TransactionAttributeUsage.Tip10 && m.Data.GetVarSize() > 8).Count();

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
        public LockAssetTransaction()
          : base(TransactionType.LockAssetTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }
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

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Recipient = reader.ReadSerializable<ECPoint>();
            IsTimeLock = reader.ReadBoolean();
            LockExpiration = reader.ReadUInt32();
            LockContract = reader.ReadSerializable<UInt160>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Recipient);
            writer.Write(IsTimeLock);
            writer.Write(LockExpiration);
            writer.Write(LockContract);
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
        }
    }

}
