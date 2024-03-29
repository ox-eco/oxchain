﻿using OX.Cryptography.ECC;
using OX.IO.Caching;
using OX.IO.Wrappers;
using OX.Ledger;
using OX.Network.P2P.Payloads;

namespace OX.Persistence
{
    internal class CloneSnapshot : Snapshot
    {
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

        public CloneSnapshot(Snapshot snapshot)
        {
            this.PersistingBlock = snapshot.PersistingBlock;
            this.Blocks = snapshot.Blocks.CreateSnapshot();
            this.Transactions = snapshot.Transactions.CreateSnapshot();
            this.Sides = snapshot.Sides.CreateSnapshot();
            this.NFTs = snapshot.NFTs.CreateSnapshot();
            this.NFTTransfers = snapshot.NFTTransfers.CreateSnapshot();
            this.Accounts = snapshot.Accounts.CreateSnapshot();
            this.Books = snapshot.Books.CreateSnapshot();
            this.UnspentCoins = snapshot.UnspentCoins.CreateSnapshot();
            this.SpentCoins = snapshot.SpentCoins.CreateSnapshot();
            this.Validators = snapshot.Validators.CreateSnapshot();
            this.Assets = snapshot.Assets.CreateSnapshot();
            this.Contracts = snapshot.Contracts.CreateSnapshot();
            this.Storages = snapshot.Storages.CreateSnapshot();
            this.HeaderHashList = snapshot.HeaderHashList.CreateSnapshot();
            this.BlockBonusVoteList = snapshot.BlockBonusVoteList.CreateSnapshot();
            this.ValidatorsCount = snapshot.ValidatorsCount.CreateSnapshot();
            this.BlockHashIndex = snapshot.BlockHashIndex.CreateSnapshot();
            this.HeaderHashIndex = snapshot.HeaderHashIndex.CreateSnapshot();
        }
    }
}
