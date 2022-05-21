using OX.Bapps;
using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Data.LevelDB;
using OX.Ledger;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.SmartContract;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Bapps
{
    public abstract class BaseBappProvider : IBappProvider
    {
        public static string WalletIndexDirectory { get; set; }
        public Bapp Bapp { get; set; }
        public abstract Wallet Wallet { get; set; }
      
        public DB Db { get; protected set; }

        public BaseBappProvider(Bapp bapp)
        {
            this.Bapp = bapp;
        }

        #region IBappProvider

        public abstract void OnBappEvent(BappEvent bappEvent);

        public abstract void OnCrossBappMessage(CrossBappMessage message);

        public abstract void OnRebuild(Wallet wallet);

        public abstract void BeforeOnBlock(Block block)
      ;
        public abstract void AfterOnBlock(Block block)
       ;
        public abstract void OnBlock(Block block);
        #endregion
        public IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, byte[] keys = default) where K : ISerializable, new() where V : ISerializable, new()
        {
            var builder = SliceBuilder.Begin(prefix);
            if (keys != default)
                builder = builder.Add(keys);
            return this.Db.Find(ReadOptions.Default, builder, (k, v) =>
            {
                var ks = k.ToArray();
                var length = ks.Length - sizeof(byte);
                ks = ks.TakeLast(length).ToArray();
                byte[] data = v.ToArray();
                return new KeyValuePair<K, V>(ks.AsSerializable<K>(), data.AsSerializable<V>());
            });
        }
        public IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, ISerializable key) where K : ISerializable, new() where V : ISerializable, new()
        {
            return GetAll<K, V>(prefix, key.IsNotNull() ? key.ToArray() : default);
        }
        public T Get<T>(byte prefix, byte[] keys) where T : ISerializable, new()
        {
            Slice value;
            if (this.Db.TryGet(ReadOptions.Default, SliceBuilder.Begin(prefix).Add(keys), out value))
            {
                byte[] data = value.ToArray();
                return data.AsSerializable<T>();
            }
            else
                return default;
        }
        public T Get<T>(byte prefix, ISerializable key) where T : ISerializable, new()
        {
            return Get<T>(prefix, key.ToArray());
        }

    }
}
