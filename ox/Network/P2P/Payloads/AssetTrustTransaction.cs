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

namespace OX.Network.P2P.Payloads
{
    public class AssetTrustTransaction : Transaction
    {
        public ECPoint Trustee;
        public ECPoint Truster;
        public bool IsMustRelateTruster;
        public UInt160[] Targets;
        public UInt160[] SideScopes;
        public UInt160 TrustContract;

        public override int Size => base.Size + Trustee.Size + Truster.Size + sizeof(bool) + Targets.GetVarSize() + SideScopes.GetVarSize() + TrustContract.Size;
        public override Fixed8 SystemFee => AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();

        public AssetTrustTransaction()
          : base(TransactionType.AssetTrustTransaction)
        {
            this.TrustContract = Blockchain.TrustAssetContractScriptHash;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Targets = new UInt160[0];
            this.SideScopes = new UInt160[0];
        }


        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Trustee = reader.ReadSerializable<ECPoint>();
            Truster = reader.ReadSerializable<ECPoint>();
            IsMustRelateTruster = reader.ReadBoolean();
            Targets = reader.ReadSerializableArray<UInt160>();
            SideScopes = reader.ReadSerializableArray<UInt160>();
            TrustContract = reader.ReadSerializable<UInt160>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Trustee);
            writer.Write(Truster);
            writer.Write(IsMustRelateTruster);
            writer.Write(Targets);
            writer.Write(SideScopes);
            writer.Write(TrustContract);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["trustee"] = Trustee.ToString();
            json["truster"] = Truster.ToString();
            json["ismustrelatetruster"] = IsMustRelateTruster.ToString();
            json["trustcontract"] = TrustContract.ToString();
            return json;
        }
        public Contract GetContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(this.Trustee);
                sb.EmitPush(this.Truster);
                byte[] targetDatas = new byte[0];
                foreach (var target in this.Targets.OrderBy(p => p))
                {
                    targetDatas = targetDatas.Concat(target.ToArray()).ToArray();
                }
                sb.EmitPush(targetDatas);
                sb.EmitPush(this.IsMustRelateTruster);
                var sideDatas = new byte[0];
                foreach (var sideScope in this.SideScopes.OrderBy(p => p))
                {
                    sideDatas = sideDatas.Concat(sideScope.ToArray()).ToArray();
                }
                sb.EmitPush(sideDatas);
                sb.EmitAppCall(this.TrustContract);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Targets.Length == 0) return false;
            if (this.Outputs.Length > 2) return false;
            var contract = GetContract();
            var output = this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash));
            if (output.IsNull()) return false;
            if (output.AssetId.Equals(Blockchain.OXS)) return false;
            return base.Verify(snapshot, mempool);
        }
    }

}
