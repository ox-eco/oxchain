//using Org.BouncyCastle.Math.EC;
using OX.Cryptography;
using OX.IO;
using OX.IO.Caching;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using OX.SmartContract;
using OX.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OX.Cryptography.ECC;
using Org.BouncyCastle.Security.Certificates;
using Nethereum.Signer;
using Nethereum.Signer.Crypto;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OX.Network.P2P.Payloads
{
    public abstract class FlashDirectcast : FlashMessage
    {
        public ECPoint Sender;

        public override int Size => base.Size + Sender.Size;
        protected FlashDirectcast(FlashMessageType type) : base(type)
        {

        }
        protected abstract void DeserializeExclusiveDataForCast(BinaryReader reader);
        protected abstract void SerializeExclusiveDataForCast(BinaryWriter writer);
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Sender = reader.ReadSerializable<ECPoint>();
            DeserializeExclusiveDataForCast(reader);
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Sender);
            SerializeExclusiveDataForCast(writer);
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return [Contract.CreateSignatureRedeemScript(this.Sender).ToScriptHash()];
        }
        public override bool SignatureVerify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            var sh = Contract.CreateSignatureRedeemScript(this.Sender).ToScriptHash();
            if (!Blockchain.Singleton.VerifyFlashMessageSender(snapshot, sh, out accountState)) return false;
            return this.VerifyWitnesses(snapshot);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["sender"] = Sender.ToString();
            return json;
        }

    }
}
