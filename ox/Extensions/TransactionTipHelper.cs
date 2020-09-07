using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Ledger;
using OX.Plugins;
using OX.Persistence;
using System.IO;
using OX.BizSystems;

namespace OX
{
    public static class TransactionTipHelper
    {
        public static bool IsContainAttributeUsage(this Transaction tx, TransactionAttributeUsage usage)
        {
            if (tx.Attributes == default || tx.Attributes.Length == 0)
                return false;
            foreach (var attr in tx.Attributes)
                if (attr.Usage == usage)
                    return true;
            return false;
        }
        public static TransactionAgentTip[] GetAgentTips(this Transaction tx)
        {
            try
            {
                if (tx.Attributes == default || tx.Attributes.Length == 0)
                    return default;
                List<TransactionAgentTip> tips = new List<TransactionAgentTip>();
                foreach (var attr in tx.Attributes)
                {
                    if (attr.Usage == TransactionAttributeUsage.AgentTip)
                    {
                        tips.Add(attr.Data.AsSerializable<TransactionAgentTip>());
                    }
                }
                return tips.ToArray();
            }
            catch
            {
                return default;
            }
        }
        public static TransactionTip[] GetTips(this Transaction tx, UInt160[] permits = null, TransactionAttributeUsage? tipUsage = null)
        {
            try
            {
                if (tx.Attributes == default || tx.Attributes.Length == 0)
                    return default;
                List<TransactionTip> tips = new List<TransactionTip>();
                foreach (var attr in tx.Attributes)
                {
                    if (attr.Usage >= TransactionAttributeUsage.Tip1 && attr.Usage <= TransactionAttributeUsage.Tip10)
                    {
                        if (tipUsage == null || attr.Usage == tipUsage.Value)
                        {
                            var tip = attr.Data.AsSerializable<TransactionTip>();
                            if (permits.IsNullOrEmpty() || permits.Contains(tip.BizScriptHash))
                                tips.Add(tip);
                        }
                    }
                }
                return tips.ToArray();
            }
            catch
            {
                return default;
            }
        }
        public static bool Verify(this TransactionTip tip, Snapshot snapshot)
        {
            if (tip.MaxIndex == 0)
            {
                return true;
            }
            bool ok = tip.MaxIndex > snapshot.Height;
            return ok;
        }
        public static bool VerifyTips(this Transaction tx, Snapshot snapshot)
        {
            var tips = tx.GetTips();
            if (tips.IsNullOrEmpty())
            {
                //OX.Plugins.Plugin.Log(nameof(OX.Consensus.ConsensusService), LogLevel.Info, $"verify tips tips is null");
                return true;
            }
            foreach (var tip in tips)
            {
                if (!tip.Verify(snapshot))
                    return false;
            }
            //OX.Plugins.Plugin.Log(nameof(OX.Consensus.ConsensusService), LogLevel.Info, $"verify tips tips is true");
            return true;
        }
    }
}
