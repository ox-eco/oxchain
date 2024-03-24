using OX.Network.P2P.Payloads;
using OX.Wallets;
using OX.IO;
using System.Collections.Generic;

namespace OX.Bapps
{
    public interface IBappProvider : IBappPort
    {
        Wallet Wallet { get; set; }
        void OnFlashState(FlashMessage flashState);
        void OnBlock(Block block);
        void BeforeOnBlock(Block block);
        void AfterOnBlock(Block block);
        void OnRebuild(Wallet wallet = null);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);

        IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, byte[] keys = default) where K : ISerializable, new() where V : ISerializable, new();
        IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, ISerializable key) where K : ISerializable, new() where V : ISerializable, new();

        T Get<T>(byte prefix, byte[] keys) where T : ISerializable, new();
        T Get<T>(byte prefix, ISerializable key) where T : ISerializable, new();
    }

}
