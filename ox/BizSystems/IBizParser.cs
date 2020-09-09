using System;
using OX.Persistence;
using System.Collections.Generic;
using static OX.Ledger.Blockchain;
using OX.Network.P2P.Payloads;
using System.Security.Permissions;
using OX.Wallets;

namespace OX.BizSystems
{
    public interface IBizParser
    {
        Wallet Wallet { get; set; }
        event EventHandler<BizEvent> BizEvent;
        //event EventHandler<TipEvent> TipEvent;
        event EventHandler<BlockEvent> BlockEvent;
        void OnBlock(Block block);
        void OnRebuild(Wallet wallet = null);
    }
    public class BizEvent : EventArgs
    {
        public UInt160 BizScriptHash;
        public Block Block;
        public BizTransaction BizTransaction;
        public ushort? N;
        public object Tag;
    }
    //public class TipEvent : EventArgs
    //{
    //    public UInt160 BizScriptHash;
    //    public Block Block;
    //    public Transaction Transaction;
    //    public ushort? N;
    //    public TransactionAttributeUsage TipType;
    //    //public TransactionTip Tip;
    //}
    public class BlockEvent : EventArgs
    {
        /// <summary>
        /// 0:normal,1:rebuild index,2:watch balance changed,3:event
        /// </summary>
        public byte EventType = 0x00;
        //public UInt160 BizScriptHash;
        public Block Block;
    }
}
