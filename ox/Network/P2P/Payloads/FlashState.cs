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
    public class FlashState : FlashMessage
    {
        public const int MaxTextDataSize = 1024;
        public static int MaxImageDataSize { get { return 1024 * (Blockchain.Singleton.GetFlashMessageSizeMutiple() - 2); } }

        public byte[] TextData;
        public byte[] ImageData;
        public override int Size => base.Size + TextData.GetVarSize() + ImageData.GetVarSize();
        public FlashState() : base(FlashMessageType.FlashState)
        {
            this.ContentType = FlashMessageContentType.Mix;
            TextData = new byte[] { 0x00 };
            ImageData = new byte[] { 0x00 };
        }
        public FlashState(ECPoint sender, uint minIndex, byte[] textData, byte[] imageData) : this()
        {
            this.Sender = sender;
            this.MinIndex = minIndex;
            if (textData.IsNotNullAndEmpty())
                this.TextData = textData;
            if (imageData.IsNotNullAndEmpty())
                this.ImageData = imageData;
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            TextData = reader.ReadVarBytes();
            ImageData = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(TextData);
            writer.WriteVarBytes(ImageData);
        }
        public override bool Verify(Snapshot snapshot, FlashMessagePool flashStatePool, out AccountState accountState)
        {
            accountState = null;
            if (TextData.Length > MaxTextDataSize) return false;
            if (ImageData.Length > MaxImageDataSize) return false;
            if (this.ContentType != FlashMessageContentType.Mix) return false;
            return base.Verify(snapshot, flashStatePool, out accountState);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["textdata"] = TextData.ToHexString();
            json["imagedata"] = ImageData.ToHexString();
            return json;
        }
    }
}
