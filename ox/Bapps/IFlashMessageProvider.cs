﻿using OX.Network.P2P.Payloads;
using OX.Wallets;
using OX.IO;
using System.Collections.Generic;

namespace OX.Bapps
{
    public interface IFlashMessageProvider : IBappPort
    {
        Wallet Wallet { get; set; }

        void OnFlashMessage(FlashMessage flashMessage);
        IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, byte[] keys = default) where K : ISerializable, new() where V : ISerializable, new();
        IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, ISerializable key) where K : ISerializable, new() where V : ISerializable, new();

        T Get<T>(byte prefix, byte[] keys) where T : ISerializable, new();
        T Get<T>(byte prefix, ISerializable key) where T : ISerializable, new();
    }

}
