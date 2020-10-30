using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Ledger
{
    public class AccountState : StateBase, ICloneable<AccountState>
    {
        public UInt160 ScriptHash;
        public DetainStatus DetainState;
        public uint DetainExpire;
        public uint AskFee;
        public ECPoint[] Votes;
        public Dictionary<UInt256, Fixed8> Balances;

        public override int Size => base.Size + ScriptHash.Size + sizeof(DetainStatus) + sizeof(uint) + sizeof(uint) + Votes.GetVarSize()
            + IO.Helper.GetVarSize(Balances.Count) + Balances.Count * (32 + 8);

        public AccountState() { }

        public AccountState(UInt160 hash)
        {
            this.ScriptHash = hash;
            this.DetainState = DetainStatus.UnFreeze;
            this.DetainExpire = 0;
            this.AskFee = 0;
            this.Votes = new ECPoint[0];
            this.Balances = new Dictionary<UInt256, Fixed8>();
        }

        AccountState ICloneable<AccountState>.Clone()
        {
            return new AccountState
            {
                ScriptHash = ScriptHash,
                DetainState = DetainState,
                DetainExpire = DetainExpire,
                AskFee = AskFee,
                Votes = Votes,
                Balances = Balances.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ScriptHash = reader.ReadSerializable<UInt160>();
            DetainState = (DetainStatus)reader.ReadByte();
            DetainExpire = reader.ReadUInt32();
            AskFee = reader.ReadUInt32();
            Votes = new ECPoint[reader.ReadVarInt()];
            for (int i = 0; i < Votes.Length; i++)
                Votes[i] = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            int count = (int)reader.ReadVarInt();
            Balances = new Dictionary<UInt256, Fixed8>(count);
            for (int i = 0; i < count; i++)
            {
                UInt256 assetId = reader.ReadSerializable<UInt256>();
                Fixed8 value = reader.ReadSerializable<Fixed8>();
                Balances.Add(assetId, value);
            }
        }

        void ICloneable<AccountState>.FromReplica(AccountState replica)
        {
            ScriptHash = replica.ScriptHash;
            DetainState = replica.DetainState;
            DetainExpire = replica.DetainExpire;
            AskFee = replica.AskFee;
            Votes = replica.Votes;
            Balances = replica.Balances;
        }

        public Fixed8 GetBalance(UInt256 asset_id)
        {
            if (!Balances.TryGetValue(asset_id, out Fixed8 value))
                value = Fixed8.Zero;
            return value;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ScriptHash);
            writer.Write((byte)DetainState);
            writer.Write(DetainExpire);
            writer.Write(AskFee);
            writer.Write(Votes);
            var balances = Balances.Where(p => p.Value > Fixed8.Zero).ToArray();
            writer.WriteVarInt(balances.Length);
            foreach (var pair in balances)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script_hash"] = ScriptHash.ToString();
            json["detainstate"] = DetainState.Value();
            json["detainexpire"] = DetainExpire.ToString();
            json["magic"] = AskFee.ToString();
            json["votes"] = Votes.Select(p => (JObject)p.ToString()).ToArray();
            json["balances"] = Balances.Select(p =>
            {
                JObject balance = new JObject();
                balance["asset"] = p.Key.ToString();
                balance["value"] = p.Value.ToString();
                return balance;
            }).ToArray();
            return json;
        }
    }
}
