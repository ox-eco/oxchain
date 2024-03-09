#pragma warning disable CS0612

using OX.IO.Caching;

namespace OX.Network.P2P.Payloads
{
    public enum FlashStateType : byte
    {
        [ReflectionCache(typeof(FlashLog))]
        FlashLog = 0x00,
    }
}
