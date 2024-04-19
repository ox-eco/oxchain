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
using OX.Wallets;
using System.Xml.Linq;

namespace OX.Network.P2P.Payloads
{
    public class FlashStateTag : ISerializable
    {
        public byte[] Data;
        public virtual int Size => Data.GetVarSize();
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }
        public FlashStateTag()
        {

        }
        public FlashStateTag(string tag) : this()
        {
            this.Data = System.Text.Encoding.UTF8.GetBytes(tag);
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            Data = reader.ReadVarBytes();
        }
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["data"] = Data.ToHexString();
            return json;
        }
        public override bool Equals(object obj)
        {
            if (obj is FlashStateTag fst)
            {
                return fst.Data.SequenceEqual(this.Data);
            }
            return base.Equals(obj);
        }
        public static bool operator ==(FlashStateTag left, FlashStateTag right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        public static bool operator !=(FlashStateTag left, FlashStateTag right)
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return this.Data.ToInt32(0);
        }
        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(this.Data);
        }
    }
    public class FlashState : FlashBroadcast
    {
        public const int MaxTagSize = 20;
        public const int MaxTagsNumber = 5;
        public const int MaxTextDataSize = 1024;
        public static int MaxImageDataSize { get { return 1024 * (Blockchain.Singleton.GetFlashMessageSizeMutiple() - 2); } }

        public byte[] TextData;
        public byte[] ImageData;
        public FlashStateTag[] Tags;
        public override int Size => base.Size + TextData.GetVarSize() + ImageData.GetVarSize() + Tags.GetVarSize();
        public FlashState() : base(FlashMessageType.FlashState)
        {
            this.ContentType = FlashMessageContentType.Mix;
            TextData = new byte[] { 0x00 };
            ImageData = new byte[] { 0x00 };
            Tags = new FlashStateTag[0];
        }
        public FlashState(UInt160 author, uint minIndex, byte[] textData, byte[] imageData, FlashStateTag[] tags) : this()
        {
            this.Author = author;
            this.MinIndex = minIndex;
            if (textData.IsNotNullAndEmpty())
                this.TextData = textData;
            if (imageData.IsNotNullAndEmpty())
                this.ImageData = imageData;
            if (tags.IsNotNullAndEmpty())
                this.Tags = tags;
        }
        protected override void DeserializeExclusiveDataForBroadcast(BinaryReader reader)
        {
            TextData = reader.ReadVarBytes();
            ImageData = reader.ReadVarBytes();
            Tags = reader.ReadSerializableArray<FlashStateTag>();
        }

        protected override void SerializeExclusiveDataForBroadcast(BinaryWriter writer)
        {
            writer.WriteVarBytes(TextData);
            writer.WriteVarBytes(ImageData);
            writer.Write(Tags);
        }
        public override bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (TextData.Length > MaxTextDataSize) return false;
            if (ImageData.Length > MaxImageDataSize) return false;
            if (this.ContentType != FlashMessageContentType.Mix) return false;
            if (this.Tags.IsNotNullAndEmpty())
            {
                if (Tags.Length > MaxTagsNumber) return false;
                foreach (var tag in Tags)
                {
                    if (tag.Size > MaxTagSize) return false;
                }
            }
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["textdata"] = TextData.ToHexString();
            json["imagedata"] = ImageData.ToHexString();
            json["tags"] = new JArray(Tags.Select(p => p.ToJson()).ToArray());
            return json;
        }
    }
}
