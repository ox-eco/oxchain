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

    public class MutualLockSellerTransaction : Transaction
    {
        public ECPoint Arbiter;
        public ECPoint Seller;
        public ECPoint Buyer;
        public UInt256 AssetId;
        public UInt256 ApproveHash;
        public uint Amount;
        public uint ExpireTimestamp;
        public UInt160 LockContract;
        public UInt256 ApproveSource;

        public override int Size => base.Size + Arbiter.Size + Seller.Size + Buyer.Size + AssetId.Size + ApproveHash.Size + sizeof(uint) + sizeof(uint) + LockContract.Size + ApproveSource.Size;
        public override Fixed8 SystemFee => AttributesFee + OutputFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark1 && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;


        public MutualLockSellerTransaction()
          : base(TransactionType.MutualLockSellerTransaction)
        {
            this.LockContract = Blockchain.MutualLockContractScriptHash;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.Add(Contract.CreateSignatureRedeemScript(this.Seller).ToScriptHash());
            return hashes.OrderBy(p => p).ToArray();
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Arbiter = reader.ReadSerializable<ECPoint>();
            Seller = reader.ReadSerializable<ECPoint>();
            Buyer = reader.ReadSerializable<ECPoint>();
            AssetId = reader.ReadSerializable<UInt256>();
            ApproveHash = reader.ReadSerializable<UInt256>();
            Amount = reader.ReadUInt32();
            ExpireTimestamp = reader.ReadUInt32();
            LockContract = reader.ReadSerializable<UInt160>();
            ApproveSource = reader.ReadSerializable<UInt256>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Arbiter);
            writer.Write(Seller);
            writer.Write(Buyer);
            writer.Write(AssetId);
            writer.Write(ApproveHash);
            writer.Write(Amount);
            writer.Write(ExpireTimestamp);
            writer.Write(LockContract);
            writer.Write(ApproveSource);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["arbiter"] = Arbiter.ToString();
            json["seller"] = Seller.ToString();
            json["buyer"] = Buyer.ToString();
            json["assetid"] = AssetId.ToString();
            json["approvehash"] = ApproveHash.ToString();
            json["amount"] = Amount.ToString();
            json["expiretimestamp"] = ExpireTimestamp.ToString();
            json["lockcontract"] = LockContract.ToString();
            json["approvesource"] = ApproveSource.ToString();
            return json;
        }
        public Contract GetContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(this.Arbiter);
                sb.EmitPush(this.Seller);
                sb.EmitPush(this.Buyer);
                sb.EmitPush(this.AssetId);
                sb.EmitPush(this.ApproveHash);
                sb.EmitPush(this.Amount);
                sb.EmitPush(this.ExpireTimestamp);
                sb.EmitAppCall(this.LockContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.LockContract != Blockchain.MutualLockContractScriptHash) return false;
            if (this.Outputs.Length == 0 || this.Outputs.Length > 2) return false;
            if (this.Seller.Equals(this.Buyer)) return false;
            var contract = GetContract();
            if (this.Outputs.Count(m => m.ScriptHash == contract.ScriptHash) != 1) return false;
            var output = this.Outputs.FirstOrDefault(m => m.ScriptHash == contract.ScriptHash && m.AssetId == this.AssetId && m.Value == Fixed8.One * this.Amount * 2);
            if (output.IsNull()) return false;
            return base.Verify(snapshot, mempool);
        }
        public UInt256 RebuildApproveHash(KeyPair key, out UInt256 approveCode)
        {
            return key.RebuildApproveHash(this.ApproveSource, out approveCode);
        }
    }

}
