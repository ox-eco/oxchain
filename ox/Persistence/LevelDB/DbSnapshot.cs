﻿using OX.Cryptography.ECC;
using OX.IO.Caching;
using OX.IO.Data.LevelDB;
using OX.IO.Wrappers;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using LSnapshot = OX.IO.Data.LevelDB.Snapshot;

namespace OX.Persistence.LevelDB
{
    internal class DbSnapshot : Snapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly WriteBatch batch;

        public override DataCache<UInt256, BlockState> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, SideSateList> Sides { get; }
        public override DataCache<NftID, NFCState> NFTs { get; }
        public override DataCache<NFSStateKey, NFSState> NFTTransfers { get; }
        public override DataCache<UInt256, BookState> Books { get; }
        public override DataCache<UInt160, AccountState> Accounts { get; }
        public override DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public override DataCache<UInt256, SpentCoinState> SpentCoins { get; }
        public override DataCache<ECPoint, ValidatorState> Validators { get; }
        public override DataCache<UInt256, AssetState> Assets { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override DataCache<UInt32Wrapper, BlockBonusVoteList> BlockBonusVoteList { get; }
        public override MetaDataCache<ValidatorsCountState> ValidatorsCount { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(DB db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            Blocks = new DbCache<UInt256, BlockState>(db, options, batch, Prefixes.DATA_Block);
            Transactions = new DbCache<UInt256, TransactionState>(db, options, batch, Prefixes.DATA_Transaction);
            Sides = new DbCache<UInt160, SideSateList>(db, options, batch, Prefixes.DATA_SideList);
            NFTs = new DbCache<NftID, NFCState>(db, options, batch, Prefixes.DATA_NFT);
            NFTTransfers = new DbCache<NFSStateKey, NFSState>(db, options, batch, Prefixes.DATA_NFT_Transfer);
            Accounts = new DbCache<UInt160, AccountState>(db, options, batch, Prefixes.ST_Account);
            Books = new DbCache<UInt256, BookState>(db, options, batch, Prefixes.DATA_Book);
            UnspentCoins = new DbCache<UInt256, UnspentCoinState>(db, options, batch, Prefixes.ST_Coin);
            SpentCoins = new DbCache<UInt256, SpentCoinState>(db, options, batch, Prefixes.ST_SpentCoin);
            Validators = new DbCache<ECPoint, ValidatorState>(db, options, batch, Prefixes.ST_Validator);
            Assets = new DbCache<UInt256, AssetState>(db, options, batch, Prefixes.ST_Asset);
            Contracts = new DbCache<UInt160, ContractState>(db, options, batch, Prefixes.ST_Contract);
            Storages = new DbCache<StorageKey, StorageItem>(db, options, batch, Prefixes.ST_Storage);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, options, batch, Prefixes.IX_HeaderHashList);
            BlockBonusVoteList = new DbCache<UInt32Wrapper, BlockBonusVoteList>(db, options, batch, Prefixes.IX_BlockBonusVoteList);
            ValidatorsCount = new DbMetaDataCache<ValidatorsCountState>(db, options, batch, Prefixes.IX_ValidatorsCount);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentHeader);
        }

        public override void Commit()
        {
            base.Commit();
            db.Write(WriteOptions.Default, batch);
        }

        public override void Dispose()
        {
            snapshot.Dispose();
        }
    }
}
