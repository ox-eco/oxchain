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
using System.Security.Claims;

namespace OX.Network.P2P.Payloads
{
    public class StateComment : ISerializable
    {
        public UInt256 StateHash;
        public UInt256 ParentCommentHash;
        public byte[] Data;
        public virtual int Size => StateHash.Size + ParentCommentHash.Size + Data.GetVarSize();
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
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(StateHash);
            writer.Write(ParentCommentHash);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            StateHash = reader.ReadSerializable<UInt256>();
            ParentCommentHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["statehash"] = StateHash.ToString();
            json["parentcommenthash"] = ParentCommentHash.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }
    }
    public class FlashStateComment : FlashBroadcast
    {
        public static int MaxCommentNumber = Blockchain.Singleton.GetFlashMessageSizeMutiple() - 1;
        public const int MaxFlashStateCommentSize = 1024;
        public StateComment[] Comments;
        public override int Size => base.Size + Comments.GetVarSize();
        public FlashStateComment() : base(FlashMessageType.FlashStateComment)
        {
        }
        public FlashStateComment(UInt160 author, uint minIndex, StateComment[] comments) : this()
        {
            this.Author = author;
            this.MinIndex = minIndex;
            this.Comments = comments;
            this.ContentType = FlashMessageContentType.Text;
        }
        protected override void DeserializeExclusiveDataForBroadcast(BinaryReader reader)
        {
            Comments = reader.ReadSerializableArray<StateComment>();
        }

        protected override void SerializeExclusiveDataForBroadcast(BinaryWriter writer)
        {
            writer.Write(Comments);
        }
        public override bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (Comments.IsNullOrEmpty()) return false;
            if (Comments.Length > MaxCommentNumber) return false;
            foreach (var comment in Comments)
            {
                if (comment.Size > MaxFlashStateCommentSize) return false;
            }
            if (this.ContentType != FlashMessageContentType.Text) return false;
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["comments"] = new JArray(Comments.Select(p => p.ToJson()).ToArray());
            return json;
        }
    }
}
