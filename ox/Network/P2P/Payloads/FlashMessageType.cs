﻿#pragma warning disable CS0612

using OX.IO.Caching;

namespace OX.Network.P2P.Payloads
{
    public enum FlashMessageContentType : byte
    {
        Text = 0x00,
        Image = 0x01
    }
    public enum FlashMessageType : byte
    {
        [ReflectionCache(typeof(FlashState))]
        FlashState = 0x00,
        [ReflectionCache(typeof(FlashMulticast))]
        FlashMulticast = 0x01,
        [ReflectionCache(typeof(FlashUnicast))]
        FlashUnicast = 0x02,
        [ReflectionCache(typeof(FlashMulticastNotice))]
        FlashMulticastNotice = 0x03,
    }
}
