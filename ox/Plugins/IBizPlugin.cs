using System;
using OX.Persistence;
using System.Collections.Generic;
using static OX.Ledger.Blockchain;
using OX.Network.P2P.Payloads;
using System.Security.Permissions;

namespace OX.Plugins
{
    public interface IBizPlugin
    {
        event EventHandler<BizEvent> BizEvent;
        event EventHandler<TipEvent> TipEvent;
        event EventHandler<BlockEvent> BlockEvent;
        void OnBlock(Block block);
        void OnRebuild();
    }
    public class BizEvent : EventArgs
    {
        public string PluginName;
        public Block Block;
        public BillTransaction Transaction;
        public BizRecordModel RecordModel;
    }
    public class TipEvent : EventArgs
    {
        public string PluginName;
        public Block Block;
        public Transaction Transaction;
        public ushort? N;
        public TransactionAttributeUsage TipType;
        public TransactionTip Tip;
    }
    public class BlockEvent : EventArgs
    {
        /// <summary>
        /// 0:normal,1:rebuild index,2:watch balance changed
        /// </summary>
        public byte EventType = 0x00;
        public string PluginName;
        public Block Block;
    }
}
