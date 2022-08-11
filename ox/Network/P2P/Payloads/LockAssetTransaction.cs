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
    public class LockAssetTransaction : Transaction
    {
        public ECPoint Recipient;
        public bool IsTimeLock;
        public uint LockExpiration;
        public UInt160 LockContract;

        public override int Size => base.Size + Recipient.Size + sizeof(bool) + sizeof(uint) + LockContract.Size;
        public override Fixed8 SystemFee => AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage <= TransactionAttributeUsage.Tip10 && m.Data.GetVarSize() > 8).Count();

        public LockAssetTransaction()
          : base(TransactionType.LockAssetTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
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
            return base.Verify(snapshot, mempool);
        }
    }

}
