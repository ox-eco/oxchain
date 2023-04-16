using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Wallets;
using System.IO;
using System.Linq;

namespace OX.Ledger
{
    public class SideSateList : StateBase, ICloneable<SideSateList>
    {
        public SideState[] SideStateList;

        public override int Size => base.Size + SideStateList.GetVarSize();
        public SideSateList()
        {
            SideStateList = new SideState[0];
        }
        SideSateList ICloneable<SideSateList>.Clone()
        {
            return new SideSateList
            {
                SideStateList = SideStateList
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            SideStateList = reader.ReadSerializableArray<SideState>();
        }

        void ICloneable<SideSateList>.FromReplica(SideSateList replica)
        {
            SideStateList = replica.SideStateList;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SideStateList);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["sidestatelist"] = SideStateList.Select(p => p.ToJson()).ToArray();
            return json;
        }
    }
    public class SideState : StateBase, ICloneable<SideState>
    {
        public UInt160 SideScriptHash;
        public SideTransaction SideTransaction;

        public override int Size => base.Size + SideScriptHash.Size + SideTransaction.Size;

        SideState ICloneable<SideState>.Clone()
        {
            return new SideState
            {
                SideScriptHash = SideScriptHash,
                SideTransaction = SideTransaction
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            SideScriptHash = reader.ReadSerializable<UInt160>();
            SideTransaction = reader.ReadSerializable<SideTransaction>();
        }

        void ICloneable<SideState>.FromReplica(SideState replica)
        {
            SideScriptHash = replica.SideScriptHash;
            SideTransaction = replica.SideTransaction;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SideScriptHash);
            writer.Write(SideTransaction);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["sidescripthash"] = SideScriptHash.ToAddress();
            json["sidetransaction"] = SideTransaction.ToJson();
            return json;
        }
    }
}
