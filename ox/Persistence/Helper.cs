﻿using OX.Cryptography.ECC;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Linq;

namespace OX.Persistence
{
    public static class Helper
    {
        public static bool ContainsBlock(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return false;
            return state.TrimmedBlock.IsBlock;
        }

        public static bool ContainsTransaction(this IPersistence persistence, UInt256 hash)
        {
            TransactionState state = persistence.Transactions.TryGet(hash);
            return state != null;
        }

        public static Block GetBlock(this IPersistence persistence, uint index)
        {
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetBlock(hash);
        }

        public static Block GetBlock(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            if (!state.TrimmedBlock.IsBlock) return null;
            return state.TrimmedBlock.GetBlock(persistence.Transactions);
        }

        public static IEnumerable<ValidatorState> GetEnrollments(this IPersistence persistence)
        {
            HashSet<ECPoint> sv = new HashSet<ECPoint>(Blockchain.StandbyValidators);
            return persistence.Validators.Find().Select(p => p.Value).Where(p => p.Registered || sv.Contains(p.PublicKey));
        }

        public static Header GetHeader(this IPersistence persistence, uint index)
        {
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetHeader(hash);
        }

        public static Header GetHeader(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Blocks.TryGet(hash)?.TrimmedBlock.Header;
        }

        public static UInt256 GetNextBlockHash(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            return Blockchain.Singleton.GetBlockHash(state.TrimmedBlock.Index + 1);
        }

        public static long GetSysFeeAmount(this IPersistence persistence, uint height)
        {
            return persistence.GetSysFeeAmount(Blockchain.Singleton.GetBlockHash(height));
        }

        public static long GetSysFeeAmount(this IPersistence persistence, UInt256 hash)
        {
            BlockState block_state = persistence.Blocks.TryGet(hash);
            if (block_state == null) return 0;
            return block_state.SystemFeeAmount;
        }

        public static Transaction GetTransaction(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Transactions.TryGet(hash)?.Transaction;
        }
        public static NFTState GetNFTState(this IPersistence persistence, UInt256 hash)
        {
            return persistence.NFTs.TryGet(hash);
        }
        public static NFTDonateState GetNFTDonateState(this IPersistence persistence, NFTDonateStateKey key)
        {
            return persistence.NFTDonates.TryGet(key);
        }
        public static BookState GetBookState(this IPersistence persistence, UInt256 bookId)
        {
            return persistence.Books.TryGet(bookId);
        }
        public static BookState GetBookState(this IPersistence persistence, uint index, ushort n)
        {
            var block = persistence.GetBlock(index);
            if (block.IsNull()) return default;
            if (block.Transactions.Length < n + 1) return default;
            return persistence.GetBookState(block.Transactions[n].Hash);
        }
        public static IEnumerable<BookSectionTransaction> GetBookSectionsState(this IPersistence persistence, UInt256 bookId)
        {
            var bookState = persistence.Books.TryGet(bookId);
            if (bookState.IsNull()) yield return default;
            if (bookState.Sections.IsNullOrEmpty()) yield return default;
            foreach (var section in bookState.Sections.OrderBy(m => m.Key))
            {
                var tx = persistence.GetTransaction(section.Value.Hash);
                if (tx is BookSectionTransaction bst)
                    yield return bst;
            }
        }
        public static TransactionOutput GetUnspent(this IPersistence persistence, UInt256 hash, ushort index)
        {
            UnspentCoinState state = persistence.UnspentCoins.TryGet(hash);
            if (state == null) return null;
            if (index >= state.Items.Length) return null;
            if (state.Items[index].HasFlag(CoinState.Spent)) return null;
            return persistence.GetTransaction(hash).Outputs[index];
        }

        public static IEnumerable<TransactionOutput> GetUnspent(this IPersistence persistence, UInt256 hash)
        {
            List<TransactionOutput> outputs = new List<TransactionOutput>();
            UnspentCoinState state = persistence.UnspentCoins.TryGet(hash);
            if (state != null)
            {
                Transaction tx = persistence.GetTransaction(hash);
                for (int i = 0; i < state.Items.Length; i++)
                    if (!state.Items[i].HasFlag(CoinState.Spent))
                        outputs.Add(tx.Outputs[i]);
            }
            return outputs;
        }

        public static bool IsDoubleSpend(this IPersistence persistence, Transaction tx)
        {
            if (tx.Inputs.Length == 0) return false;
            foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
            {
                UnspentCoinState state = persistence.UnspentCoins.TryGet(group.Key);
                if (state == null) return true;
                if (group.Any(p => p.PrevIndex >= state.Items.Length || state.Items[p.PrevIndex].HasFlag(CoinState.Spent)))
                    return true;
            }
            return false;
        }
    }
}
