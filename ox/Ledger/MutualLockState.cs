using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using System.IO;

namespace OX.Ledger
{
    public class MutualLockState : StateBase, ICloneable<MutualLockState>
    {
        public MutualLockSellerTransaction SellerTx;
        public bool Locked;
        public UInt256 BuyerTxHash;

        public override int Size => base.Size + SellerTx.Size + sizeof(bool) + BuyerTxHash.Size;
        public MutualLockState()
        {
            Locked = false;
            BuyerTxHash = UInt256.Zero;
        }
        MutualLockState ICloneable<MutualLockState>.Clone()
        {
            return new MutualLockState
            {
                SellerTx = SellerTx,
                Locked = Locked,
                BuyerTxHash = BuyerTxHash,
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            SellerTx = reader.ReadSerializable<MutualLockSellerTransaction>();
            Locked = reader.ReadBoolean();
            BuyerTxHash = reader.ReadSerializable<UInt256>();
        }

        void ICloneable<MutualLockState>.FromReplica(MutualLockState replica)
        {
            SellerTx = replica.SellerTx;
            Locked = replica.Locked;
            BuyerTxHash = replica.BuyerTxHash;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SellerTx);
            writer.Write(Locked);
            writer.Write(BuyerTxHash);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["sellertx"] = SellerTx.ToJson();
            json["locked"] = Locked.ToString();
            json["buyertxhash"] = BuyerTxHash.ToString();
            return json;
        }
    }
}
