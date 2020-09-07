using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Ledger;
using System.IO;

namespace OX
{
    public class TransactionAgentTip : ISerializable
    {
        public byte AgentType;
        public UInt160 Agent;
        public byte[] Data;

        public virtual int Size => sizeof(byte) + Agent.Size + Data.GetVarSize();
        public TransactionAgentTip()
        {
            this.AgentType = 0x00;
            this.Agent = UInt160.Zero;
            this.Data = new byte[] { 0x00 };
        }
        public TransactionAgentTip(byte subType, UInt160 from) : this()
        {
            this.AgentType = subType;
            this.Agent = from;
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(AgentType);
            writer.Write(Agent);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            AgentType = reader.ReadByte();
            Agent = reader.ReadSerializable<UInt160>();
            Data = reader.ReadVarBytes();
        }
        public bool GetSubAgentTipModel<T>(byte agentType, out T model) where T : ISerializable, new()
        {
            if (this.AgentType != agentType)
            {
                model = default;
                return false;
            }
            try
            {
                model = this.Data.AsSerializable<T>();
                return true;
            }
            catch
            {
                model = default;
                return false;
            }
        }
    }
}
