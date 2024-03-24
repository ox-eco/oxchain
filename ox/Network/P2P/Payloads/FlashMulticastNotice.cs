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
using static Akka.Actor.ProviderSelection;

namespace OX.Network.P2P.Payloads
{
    public class MulticastNoticeDest : ISerializable
    {
        public UInt256 RecipientHash;
        public byte[] Data;
        public virtual int Size => RecipientHash.Size + Data.GetVarSize();
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RecipientHash);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            RecipientHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }
    }
    public class FlashMulticastNotice : FlashState
    {
        public byte[] Msg;
        public MulticastNoticeDest[] Destinations;
        public override int Size => base.Size + Msg.GetVarSize() + Destinations.GetVarSize();
        public FlashMulticastNotice() : base(FlashStateType.FlashMulticastNotice)
        {

        }
        public FlashMulticastNotice(KeyPair local, uint minIndex, byte[] key, ECPoint[] destPubkeys, byte[] msg) : this()
        {
            this.Sender = local.PublicKey;
            this.MinIndex = minIndex;
            this.Msg = msg;
            List<MulticastNoticeDest> list = new List<MulticastNoticeDest>();
            foreach (var dest in destPubkeys)
            {
                list.Add(new MulticastNoticeDest()
                {
                    RecipientHash = Contract.CreateSignatureRedeemScript(dest).ToScriptHash().Hash,
                    Data = key.Encrypt(local, dest, BitConverter.GetBytes(minIndex))
                });
            }
            this.Destinations = list.ToArray();
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Msg = reader.ReadVarBytes();
            Destinations = reader.ReadSerializableArray<MulticastNoticeDest>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Msg);
            writer.Write(Destinations);
        }


        public override bool Verify(Snapshot snapshot, FlashStatePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (this.Destinations.IsNullOrEmpty()) return false;
            if (this.Destinations.Length > 40) return false;
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
    }
}
