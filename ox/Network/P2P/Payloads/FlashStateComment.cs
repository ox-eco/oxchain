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

namespace OX.Network.P2P.Payloads
{
    public class FlashStateComment : FlashMessage
    {
        public const int MaxFlashStateCommentSize = 1024;
        public UInt256 StateHash;
        public UInt256 ParentCommentHash;
        public byte[] Data;
        public override int Size => base.Size + Data.GetVarSize();
        public FlashStateComment() : base(FlashMessageType.FlashStateComment)
        {
            Data = new byte[] { 0x00 };
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            StateHash = reader.ReadSerializable<UInt256>();
            ParentCommentHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(StateHash);
            writer.Write(ParentCommentHash);
            writer.WriteVarBytes(Data);
        }
        public override bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (Size > MaxFlashStateCommentSize) return false;
            if (this.ContentType!= FlashMessageContentType.Text) return false;
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["statehash"] = StateHash.ToString();
            json["parentcommenthash"] = ParentCommentHash.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
