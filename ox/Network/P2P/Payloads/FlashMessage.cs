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

namespace OX.Network.P2P.Payloads
{
    public abstract class FlashMessage : IEquatable<FlashMessage>, IInventory
    {
        public static int MaxFlashMessageSize { get { return 1024 * Blockchain.Singleton.GetFlashMessageSizeMutiple(); } }
        /// <summary>
        /// Reflection cache for FlashStateType
        /// </summary>
        private static ReflectionCache<byte> ReflectionCache = ReflectionCache<byte>.CreateFromEnum<FlashMessageType>();

        public readonly FlashMessageType Type;
        public ECPoint Sender;
        public uint MinIndex;
        public FlashMessageContentType ContentType;
        public Witness[] Witnesses { get; set; }

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

        InventoryType IInventory.InventoryType => InventoryType.FS;



        public virtual int Size => sizeof(TransactionType) + Sender.Size + sizeof(uint) + sizeof(FlashMessageContentType) + Witnesses.GetVarSize();

        protected FlashMessage(FlashMessageType type)
        {
            this.Type = type;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>();
            OnDeserialized();
        }

        protected virtual void DeserializeExclusiveData(BinaryReader reader)
        {
        }

        public static FlashMessage DeserializeFrom(byte[] value, int offset = 0)
        {
            using (MemoryStream ms = new MemoryStream(value, offset, value.Length - offset, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return DeserializeFrom(reader);
            }
        }

        internal static FlashMessage DeserializeFrom(BinaryReader reader)
        {
            // Looking for type in reflection cache
            FlashMessage flashState = ReflectionCache.CreateInstance<FlashMessage>(reader.ReadByte());
            if (flashState == null) throw new FormatException();

            flashState.DeserializeUnsignedWithoutType(reader);
            flashState.Witnesses = reader.ReadSerializableArray<Witness>();
            flashState.OnDeserialized();
            return flashState;
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            if ((FlashMessageType)reader.ReadByte() != Type)
                throw new FormatException();
            DeserializeUnsignedWithoutType(reader);
        }

        private void DeserializeUnsignedWithoutType(BinaryReader reader)
        {
            Sender = reader.ReadSerializable<ECPoint>();
            MinIndex = reader.ReadUInt32();
            ContentType = (FlashMessageContentType)reader.ReadByte();
            DeserializeExclusiveData(reader);
        }

        public bool Equals(FlashMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FlashMessage);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        byte[] IScriptContainer.GetMessage()
        {
            return this.GetHashData();
        }

        public virtual UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return [Contract.CreateSignatureRedeemScript(this.Sender).ToScriptHash()];
        }



        protected virtual void OnDeserialized()
        {
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        protected virtual void SerializeExclusiveData(BinaryWriter writer)
        {
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Sender);
            writer.Write(MinIndex);
            writer.Write((byte)ContentType);
            SerializeExclusiveData(writer);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["fsid"] = Hash.ToString();
            json["size"] = Size;
            json["type"] = Type;
            json["sender"] = Sender.ToString();
            json["minindex"] = MinIndex.ToString();
            json["contenttype"] = ContentType.Value().ToString();
            return json;
        }

        bool IInventory.Verify(Snapshot snapshot)
        {
            return Verify(snapshot, default(FlashMessagePool), out AccountState accountState);
        }

        public virtual bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (MinIndex > snapshot.Height + 1) return false;
            if (MinIndex + 10 <= snapshot.Height) return false;
            var sh = Contract.CreateSignatureRedeemScript(this.Sender).ToScriptHash();
            if (!Blockchain.Singleton.VerifyFlashMessageSender(snapshot, sh, out accountState)) return false;
            return this.VerifyWitnesses(snapshot);
        }


    }
}
