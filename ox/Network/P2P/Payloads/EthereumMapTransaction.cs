﻿using OX.IO;
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
    public class EthereumMapTransaction : Transaction
    {
        public string EthereumAddress;
        public uint LockExpirationIndex;
        public UInt160 EthMapContract;
        public override int Size => base.Size + EthereumAddress.GetVarSize() + sizeof(uint) + EthMapContract.Size;
        public override Fixed8 SystemFee => AttributesFee;
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();

        public EthereumMapTransaction()
          : base(TransactionType.EthereumMapTransaction)
        {
            this.EthMapContract = Blockchain.EthereumMapContractScriptHash;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.LockExpirationIndex = 0;
        }


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
            if (this.EthereumAddress.IsNullOrEmpty()) return false;
            if (this.Outputs.Length > 2) return false;
            var contract = GetContract();
            var output = this.Outputs.FirstOrDefault(m => m.ScriptHash.Equals(contract.ScriptHash));
            if (output.IsNull()) return false;
            if (output.AssetId.Equals(Blockchain.OXS)) return false;
            return base.Verify(snapshot, mempool);
        }
    }

}
