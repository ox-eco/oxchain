using OX.IO;
using System.IO;
using System.Collections.Generic;

namespace OX.Network.P2P.Payloads
{
    public enum EventType : byte
    {
        Engrave = 0x01,
        Board = 0x02,
        Digg = 0x04
    }
    public enum EngraveType : byte
    {
        Words = 0x01,
        Image = 0x02,
        Symbol = 0x03
    }
    public enum DiggType : byte
    {
        Up = 1 << 0,
        Down = 1 << 1,
        None = 1 << 7
    }

    public class Board : ISerializable
    {
        public string Name;
        public long latitude;
        public long longitude;
        public string Remark;
        public byte[] Data;
        public int Size => Name.GetVarSize() + sizeof(long) + sizeof(long) + Remark.GetVarSize() + Data.GetVarSize();
        public bool IsOpen => this.Data.Length == 1 && this.Data[0] == 0x00;
        public Board()
        {
            Remark = "0";
            Data = new byte[] { 0x00 };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarString(Name);
            writer.Write(latitude);
            writer.Write(longitude);
            writer.WriteVarString(Remark);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadVarString();
            latitude = reader.ReadInt64();
            longitude = reader.ReadInt64();
            Remark = reader.ReadVarString();
            Data = reader.ReadVarBytes();
        }

    }
    public class Engrave : ISerializable
    {
        public uint BoardTxIndex;
        public ushort BoardTxPosition;
        public EngraveType EngraveType;
        public uint Timestamp;
        public UInt160[] Participants;
        public string Title;
        public string Message;
        public byte[] Data;
        public int Size => sizeof(uint) + sizeof(ushort) + sizeof(EngraveType) + sizeof(uint) + Participants.GetVarSize() + Title.GetVarSize() + Message.GetVarSize() + Data.GetVarSize();
        public bool IsOpen => this.Data.Length == 1 && this.Data[0] == 0x00;
        public Engrave()
        {
            EngraveType = EngraveType.Words;
            Participants = new UInt160[0];
            Title = "0";
            Message = "0";
            Data = new byte[] { 0x00 };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(BoardTxIndex);
            writer.Write(BoardTxPosition);
            writer.Write((byte)EngraveType);
            writer.Write(Timestamp);
            writer.Write(Participants);
            writer.WriteVarString(Title);
            writer.WriteVarString(Message);
            writer.WriteVarBytes(Data);
        }
        public void Deserialize(BinaryReader reader)
        {
            BoardTxIndex = reader.ReadUInt32();
            BoardTxPosition = reader.ReadUInt16();
            EngraveType = (EngraveType)reader.ReadByte();
            Timestamp = reader.ReadUInt32();
            Participants = reader.ReadSerializableArray<UInt160>();
            Title = reader.ReadVarString();
            Message = reader.ReadVarString();
            Data = reader.ReadVarBytes();
        }

    }
    public class Digg : ISerializable
    {
        public UInt256 EngraveId;
        public UInt256 AtDiggId;
        public DiggType DiggType;
        public uint Timestamp;
        public string Message;
        public int Size => EngraveId.Size + AtDiggId.Size + sizeof(DiggType) + sizeof(uint) + Message.GetVarSize();
        public Digg()
        {
            AtDiggId = UInt256.Zero;
            Message = "0";
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(EngraveId);
            writer.Write(AtDiggId);
            writer.Write((byte)DiggType);
            writer.Write(Timestamp);
            writer.WriteVarString(Message);
        }
        public void Deserialize(BinaryReader reader)
        {
            EngraveId = reader.ReadSerializable<UInt256>();
            AtDiggId = reader.ReadSerializable<UInt256>();
            DiggType = (DiggType)reader.ReadByte();
            Timestamp = reader.ReadUInt32();
            Message = reader.ReadVarString();
        }
    }
}
