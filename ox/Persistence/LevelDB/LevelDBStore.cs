﻿using OX.Cryptography.ECC;
using OX.IO.Caching;
using OX.IO.Data.LevelDB;
using OX.IO.Wrappers;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using System;
using System.Reflection;

namespace OX.Persistence.LevelDB
{
    public class LevelDBStore : Store, IDisposable
    {
        private readonly DB db;

        public LevelDBStore(string path)
        {
            this.db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(Prefixes.SYS_Version), out Slice value) && Version.TryParse(value.ToString(), out Version version) && version >= Version.Parse("1.0.2"))
                return;
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false };
            using (Iterator it = db.NewIterator(options))
            {
                for (it.SeekToFirst(); it.Valid(); it.Next())
                {
                    batch.Delete(it.Key());
                }
            }
            db.Put(WriteOptions.Default, SliceBuilder.Begin(Prefixes.SYS_Version), Assembly.GetExecutingAssembly().GetName().Version.ToString());
            db.Write(WriteOptions.Default, batch);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public override byte[] Get(byte prefix, byte[] key)
        {
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(prefix).Add(key), out Slice slice))
                return null;
            return slice.ToArray();
        }

        public override DataCache<UInt160, AccountState> GetAccounts()
        {
            return new DbCache<UInt160, AccountState>(db, null, null, Prefixes.ST_Account);
        }

        public override DataCache<UInt256, AssetState> GetAssets()
        {
            return new DbCache<UInt256, AssetState>(db, null, null, Prefixes.ST_Asset);
        }

        public override DataCache<UInt256, BlockState> GetBlocks()
        {
            return new DbCache<UInt256, BlockState>(db, null, null, Prefixes.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, null, null, Prefixes.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<UInt256, SpentCoinState> GetSpentCoins()
        {
            return new DbCache<UInt256, SpentCoinState>(db, null, null, Prefixes.ST_SpentCoin);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, null, null, Prefixes.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, null, null, Prefixes.DATA_Transaction);
        }
        public override DataCache<UInt160, SideSateList> GetSides()
        {
            return new DbCache<UInt160, SideSateList>(db, null, null, Prefixes.DATA_SideList);
        }
        
        public override DataCache<NftID, NFCState> GetNFCs()
        {
            return new DbCache<NftID, NFCState>(db, null, null, Prefixes.DATA_NFT);
        }
       
        public override DataCache<NFSStateKey, NFSState> GetNFSs()
        {
            return new DbCache<NFSStateKey, NFSState>(db, null, null, Prefixes.DATA_NFT_Transfer);
        }
        public override DataCache<UInt256, BookState> GetBooks()
        {
            return new DbCache<UInt256, BookState>(db, null, null, Prefixes.DATA_Book);
        }
        public override DataCache<UInt256, UnspentCoinState> GetUnspentCoins()
        {
            return new DbCache<UInt256, UnspentCoinState>(db, null, null, Prefixes.ST_Coin);
        }

        public override DataCache<ECPoint, ValidatorState> GetValidators()
        {
            return new DbCache<ECPoint, ValidatorState>(db, null, null, Prefixes.ST_Validator);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, null, null, Prefixes.IX_HeaderHashList);
        }
        public override DataCache<UInt32Wrapper, BlockBonusVoteList> GetBlockBonusVoteList()
        {
            return new DbCache<UInt32Wrapper, BlockBonusVoteList>(db, null, null, Prefixes.IX_BlockBonusVoteList);
        }
        public override MetaDataCache<ValidatorsCountState> GetValidatorsCount()
        {
            return new DbMetaDataCache<ValidatorsCountState>(db, null, null, Prefixes.IX_ValidatorsCount);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, Prefixes.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, Prefixes.IX_CurrentHeader);
        }

        public override void Put(byte prefix, byte[] key, byte[] value)
        {
            db.Put(WriteOptions.Default, SliceBuilder.Begin(prefix).Add(key), value);
        }

        public override void PutSync(byte prefix, byte[] key, byte[] value)
        {
            db.Put(new WriteOptions { Sync = true }, SliceBuilder.Begin(prefix).Add(key), value);
        }
    }
}
