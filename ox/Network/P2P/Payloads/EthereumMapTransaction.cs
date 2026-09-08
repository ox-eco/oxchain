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
    public class EthereumMapTransaction : Transaction
    {
        public string EthereumAddress;
        public uint LockExpirationIndex;
        public UInt160 EthMapContract;
        public override int Size => base.Size + EthereumAddress.GetVarSize() + sizeof(uint) + EthMapContract.Size;
        public override Fixed8 SystemFee => AttributesFee + OutputFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage != TransactionAttributeUsage.EthScriptHash && m.Usage >= TransactionAttributeUsage.Remark1 && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
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
        public DaoVote[] DaoVotes
        {
            get
            {
                if (this.Attributes.IsNullOrEmpty()) return default;
                var attrs = this.Attributes.Where(p => p.Usage == TransactionAttributeUsage.DaoVote);
                if (attrs.IsNullOrEmpty()) return default;
                return attrs.Select(p => p.Data.AsSerializable<DaoVote>()).ToArray();
            }
        }
        #endregion
        public EthereumMapTransaction()
          : base(TransactionType.EthereumMapTransaction)
        {
            this.EthMapContract = Blockchain.EthereumMapContractScriptHash;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.LockExpirationIndex = 0;
        }
        #region append for dao vote
        public bool TryGetDaoVote(out DaoVote vote)
        {
            vote = default;
            if (DaoVotes.IsNotNullAndEmpty() && DaoVotes.Length == 1)
            {
                vote = DaoVotes[0];
                return true;
            }
            return false;
        }
        #endregion
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
            EthereumAddress = reader.ReadVarString();
            LockExpirationIndex = reader.ReadUInt32();
            EthMapContract = reader.ReadSerializable<UInt160>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarString(EthereumAddress.ToLower());
            writer.Write(LockExpirationIndex);
            writer.Write(EthMapContract);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["ethereumaddress"] = EthereumAddress.ToLower();
            json["lockexpirationindex"] = LockExpirationIndex.ToString();
            return json;
        }
        public Contract GetContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(this.EthereumAddress.ToLower());
                sb.EmitPush(this.LockExpirationIndex);
                sb.EmitAppCall(this.EthMapContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.EthMapContract != Blockchain.EthereumMapContractScriptHash) return false;
            if (this.EthereumAddress.IsNullOrEmpty()) return false;
            if (this.Outputs.Length > 4) return false;
            var contract = GetContract();
            var output = this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash));
            if (output.IsNull()) return false;
            if (output.AssetId.Equals(Blockchain.OXS)) return false;
            #region append for dao vote
            if (this.DaoVotes.IsNotNullAndEmpty() && this.DaoVotes.Length > 1) return false;
            if (this.TryGetDaoVote(out DaoVote daoVote))
            {
                if (daoVote.Index % 10000 > 0) return false;
                if (daoVote.Index <= Blockchain.Singleton.HeaderHeight) return false;
                if (this.LockExpirationIndex < daoVote.Index) return false;
                if (this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash) && m.AssetId.Equals(daoVote.AssetId)).IsNull()) return false;
            }
            #endregion
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
                    var currentIssued = mempool.OfType<EthereumMapTransaction>().Where(p => p != this && p.IsIssue).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
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
