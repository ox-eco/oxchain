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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Nethereum.Util;
using Org.BouncyCastle.Asn1.X509;

namespace OX.Network.P2P.Payloads
{
    public class NFTPending : ISerializable
    {
        public NFSStateKey Key;
        public MixSignatureValidator<NftTransferAuthentication> Validator;

        public virtual int Size => Key.Size + Validator.Size;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Key);
            writer.Write(Validator);
        }
        public void Deserialize(BinaryReader reader)
        {
            Key = reader.ReadSerializable<NFSStateKey>();
            Validator = reader.ReadSerializable<MixSignatureValidator<NftTransferAuthentication>>();
        }

    }
    public class FlashNFTPending : FlashBroadcast
    {
        public NFTPending[] Pendings;
        public override int Size => base.Size + Pendings.GetVarSize();
        public FlashNFTPending() : base(FlashMessageType.FlashNFTPending)
        {
            this.ContentType = FlashMessageContentType.Protocol;
            Pendings = new NFTPending[0];
        }
        public FlashNFTPending(UInt160 author, uint minIndex, NFTPending[] pendings) : this()
        {
            this.Author = author;
            this.MinIndex = minIndex;
            this.Pendings = pendings;
            this.ContentType = FlashMessageContentType.Protocol;
        }
        protected override void DeserializeExclusiveDataForBroadcast(BinaryReader reader)
        {
            Pendings = reader.ReadSerializableArray<NFTPending>();
        }

        protected override void SerializeExclusiveDataForBroadcast(BinaryWriter writer)
        {
            writer.Write(Pendings);
        }
        public override bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (this.ContentType != FlashMessageContentType.Protocol) return false;
            if (this.Pendings.IsNullOrEmpty()) return false;
            foreach (var pending in Pendings)
            {
                if (pending.Validator.IsNull() || pending.Key.IsNull() || !pending.Validator.Verify()) return false;
                if (pending.Validator.Target.Amount < Fixed8.Zero || pending.Validator.Target.MaxIndex < pending.Validator.Target.MinIndex) return false;
                var nft = snapshot.GetNftState(pending.Key.NFCID);
                if ((nft.IsNull())) return false;
                var donateState = snapshot.GetNftTransfer(pending.Key);
                if (donateState.IsNull()) return false;
                if (donateState.LastNFS.Hash != pending.Validator.Target.PreHash) return false;
                if (donateState.LastNFS.NftChangeType == NftChangeType.Issue && nft.NFC.FirstResaleLock > 0)
                {
                    if (Blockchain.Singleton.Height <= pending.Key.IssueBlockIndex + nft.NFC.FirstResaleLock * 10000) return false;
                }
                try
                {
                    if (pending.Validator.Target.Target.MixAccountType == MixAccountType.OX)
                    {
                        if (Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pending.Validator.Target.Target.Target, ECCurve.Secp256r1)).ToScriptHash() != this.Author) return false;
                    }
                    else if (pending.Validator.Target.Target.MixAccountType == MixAccountType.Ethereum)
                    {
                        var address = new AddressUtil().ConvertToChecksumAddress(pending.Validator.Target.Target.Target.ToHex());
                        if (address.BuildMapAddress() != this.Author) return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
    }
}
