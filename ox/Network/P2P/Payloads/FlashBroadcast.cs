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
using Nethereum.Util;
using Org.BouncyCastle.Asn1.X509;

namespace OX.Network.P2P.Payloads
{
    public abstract class FlashBroadcast : FlashMessage
    {
        public byte[] EthSignature;
        public UInt160 Author;

        public override int Size => base.Size + EthSignature.GetVarSize() + Author.Size;
        protected FlashBroadcast(FlashMessageType type) : base(type)
        {
            this.Witnesses = new Witness[0];
            this.EthSignature = new byte[0];
        }
        protected abstract void DeserializeExclusiveDataForBroadcast(BinaryReader reader);
        protected abstract void SerializeExclusiveDataForBroadcast(BinaryWriter writer);
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            EthSignature = reader.ReadVarBytes();
            Author = reader.ReadSerializable<UInt160>();
            DeserializeExclusiveDataForBroadcast(reader);
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(EthSignature);
            writer.Write(Author);
            SerializeExclusiveDataForBroadcast(writer);
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return [this.Author];
        }
        public override bool SignatureVerify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            if (!Blockchain.Singleton.VerifyFlashMessageSender(snapshot, this.Author, out accountState)) return false;
            if (this.VerifyWitnesses(snapshot)) return true;
            if (this.EthSignature.IsNullOrEmpty()) return false;
            var stringToSign = this.GetRequireEthSignatureData().ToHex(true);
            var signer = new Nethereum.Signer.EthereumMessageSigner();
            var ethaddress = signer.EncodeUTF8AndEcRecover(stringToSign, this.EthSignature.ToHex());
            if (ethaddress.IsNullOrEmpty()) return false;
            //ethAddress.HexToByteArray()
            return ethaddress.BuildMapAddress() == this.Author;
        }
        byte[] GetRequireEthSignatureData()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                this.SerializeExclusiveDataForBroadcast(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["author"] = Author.ToString();
            return json;
        }

    }
}
