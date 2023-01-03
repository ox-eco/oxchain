using OX.IO;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public enum BookStorageType : byte
    {
        OnChain = 1 << 0,
        OutChain = 1 << 1,
        MixChain = 1 << 2
    }
    public enum BookType : byte
    {
        Common = 0x01 
    }
    public enum BookSectionType : byte
    {
        Title = 0x01,
        PS = 0x02,
        Body = 0x03
    }
    public enum BookEncodingType : byte
    {
        UTF8 = 0x01,
        Unicode = 0x02,
        ASCII = 0x03
    }    
}
