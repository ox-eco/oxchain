using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Caching;
using OX.IO.Data.LevelDB;
using OX.IO.Wrappers;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using System.Collections.Generic;

namespace OX.Persistence
{
    public abstract class Store : IPersistence
    {
        DataCache<UInt256, BlockState> IPersistence.Blocks => GetBlocks();
        DataCache<UInt256, TransactionState> IPersistence.Transactions => GetTransactions();
        DataCache<UInt160, SideSateList> IPersistence.Sides => GetSides();
        DataCache<NftID, NFCState> IPersistence.NFTs => GetNFTs();
        DataCache<NFSStateKey, NFSState> IPersistence.NFTTransfers => GetNFTTransfers();
        DataCache<UInt256, BookState> IPersistence.Books => GetBooks();
        DataCache<UInt160, AccountState> IPersistence.Accounts => GetAccounts();
        DataCache<UInt256, UnspentCoinState> IPersistence.UnspentCoins => GetUnspentCoins();
        DataCache<UInt256, SpentCoinState> IPersistence.SpentCoins => GetSpentCoins();
        DataCache<ECPoint, ValidatorState> IPersistence.Validators => GetValidators();
        DataCache<UInt256, AssetState> IPersistence.Assets => GetAssets();
        DataCache<UInt160, ContractState> IPersistence.Contracts => GetContracts();
        DataCache<StorageKey, StorageItem> IPersistence.Storages => GetStorages();
        DataCache<UInt32Wrapper, HeaderHashList> IPersistence.HeaderHashList => GetHeaderHashList();
        DataCache<UInt32Wrapper, BlockBonusVoteList> IPersistence.BlockBonusVoteList => GetBlockBonusVoteList();
        MetaDataCache<ValidatorsCountState> IPersistence.ValidatorsCount => GetValidatorsCount();
        MetaDataCache<HashIndexState> IPersistence.BlockHashIndex => GetBlockHashIndex();
        MetaDataCache<HashIndexState> IPersistence.HeaderHashIndex => GetHeaderHashIndex();

        public abstract byte[] Get(byte prefix, byte[] key);
        public abstract DataCache<UInt256, BlockState> GetBlocks();
        public abstract DataCache<UInt256, TransactionState> GetTransactions();
        public abstract DataCache<UInt160, SideSateList> GetSides();
        public abstract DataCache<NftID, NFCState> GetNFTs();
        public abstract DataCache<NFSStateKey, NFSState> GetNFTTransfers();
        public abstract DataCache<UInt256, BookState> GetBooks();
        public abstract DataCache<UInt160, AccountState> GetAccounts();
        public abstract DataCache<UInt256, UnspentCoinState> GetUnspentCoins();
        public abstract DataCache<UInt256, SpentCoinState> GetSpentCoins();
        public abstract DataCache<ECPoint, ValidatorState> GetValidators();
        public abstract DataCache<UInt256, AssetState> GetAssets();
        public abstract DataCache<UInt160, ContractState> GetContracts();
        public abstract DataCache<StorageKey, StorageItem> GetStorages();
        public abstract DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList();
        public abstract DataCache<UInt32Wrapper, BlockBonusVoteList> GetBlockBonusVoteList();
        public abstract MetaDataCache<ValidatorsCountState> GetValidatorsCount();
        public abstract MetaDataCache<HashIndexState> GetBlockHashIndex();
        public abstract MetaDataCache<HashIndexState> GetHeaderHashIndex();
        public abstract void Put(byte prefix, byte[] key, byte[] value);
        public abstract void PutSync(byte prefix, byte[] key, byte[] value);

        public abstract Snapshot GetSnapshot();
        public abstract IEnumerable<KeyValuePair<byte[], byte[]>> GetAll(byte prefix, byte[] keys = default);
        public abstract IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, byte[] keys = default) where K : ISerializable, new() where V : ISerializable, new();

        public abstract IEnumerable<KeyValuePair<K, V>> GetAll<K, V>(byte prefix, ISerializable key) where K : ISerializable, new() where V : ISerializable, new();

       


    }
}
