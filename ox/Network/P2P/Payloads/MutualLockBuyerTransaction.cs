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

    public class MutualLockBuyerTransaction : Transaction
    {
        public ECPoint Buyer;
        public UInt160 MutualLockScriptHash;
        public override int Size => base.Size + Buyer.Size + MutualLockScriptHash.Size;
        public override Fixed8 SystemFee => AttributesFee + OutputFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark1 && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => true;


        public MutualLockBuyerTransaction()
          : base(TransactionType.MutualLockBuyerTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.Add(Contract.CreateSignatureRedeemScript(this.Buyer).ToScriptHash());
            return hashes.OrderBy(p => p).ToArray();
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Buyer = reader.ReadSerializable<ECPoint>();
            MutualLockScriptHash = reader.ReadSerializable<UInt160>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Buyer);
            writer.Write(MutualLockScriptHash);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["buyer"] = Buyer.ToString();
            json["mutuallockscripthash"] = MutualLockScriptHash.ToString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Outputs.Length == 0 || this.Outputs.Length > 2) return false;
            var mutualLockState = snapshot.GetMutualLockState(this.MutualLockScriptHash);
            if (mutualLockState.IsNull()) return false;
            if (mutualLockState.Locked) return false;
            if (mutualLockState.SellerTx.Buyer != this.Buyer) return false;
            var contract = mutualLockState.SellerTx.GetContract();
            if (this.Outputs.Count(m => m.ScriptHash == contract.ScriptHash) != 1) return false;
            var output = this.Outputs.FirstOrDefault(m => m.ScriptHash == contract.ScriptHash && m.AssetId == mutualLockState.SellerTx.AssetId && m.Value == Fixed8.One * mutualLockState.SellerTx.Amount);
            if (output.IsNull()) return false;
            return base.Verify(snapshot, mempool);
        }

    }

}
