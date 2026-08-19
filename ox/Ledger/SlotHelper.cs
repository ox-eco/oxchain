using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;
using System.Collections.Generic;
using System.Linq;
using OX;

namespace OX.Ledger
{
    public static class SlotHelper
    {
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 slotScriptHash)
        {
            return blockchain.CurrentSnapshot.VerifySlotValidator(slotScriptHash, out AccountState _, out Fixed8 _, out Fixed8 _);
        }
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 slotScriptHash, out AccountState accountState, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            return blockchain.CurrentSnapshot.VerifySlotValidator(slotScriptHash, out accountState, out OXSBalance, out AskFee);
        }
        public static bool VerifySlotValidator(this Snapshot snapshot, UInt160 slotScriptHash, out AccountState accountState, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            return snapshot.OnlyVerifySlotValidator(slotScriptHash, out accountState, out OXSBalance, out AskFee)&&snapshot.ValidSlotInVote_35000000(slotScriptHash);
        }
        public static bool OnlyVerifySlotValidator(this Snapshot snapshot, UInt160 slotScriptHash, out AccountState accountState, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            OXSBalance = Fixed8.Zero;
            AskFee = Fixed8.Zero;
            accountState = snapshot.Accounts.GetAndChange(slotScriptHash, () => null);
            if (accountState.IsNull()) return false;
            var balance = accountState.GetBalance(Blockchain.OXS);
            OXSBalance = balance;
            AskFee = accountState.AskFee;
            if (accountState.SlotState == SlotStatus.UnFreeze) return false;
            if (accountState.SlotExpire < snapshot.Height) return false;
            if (balance < Blockchain.BappSlotRentOXS) return false;
            return true;
        }
        public static bool ValidSlotInVote_35000000(this Snapshot snapshot, UInt160 slotScriptHash)
        {
            bool ok = true;
            var slotOffVoteList = snapshot.SlotOffVoteList.TryGet(slotScriptHash);
            if (slotOffVoteList.IsNotNull())
            {
                var v = slotOffVoteList.Votes.FirstOrDefault(m => m.Value > Fixed8.One * 35000000);
                ok = default(KeyValuePair<uint, Fixed8>).Equals(v);
            }
            return ok;
        }
        public static bool ValidSlotInVote_50000000(this Snapshot snapshot, UInt160 slotScriptHash)
        {
            bool ok = true;
            var slotOffVoteList = snapshot.SlotOffVoteList.TryGet(slotScriptHash);
            if (slotOffVoteList.IsNotNull())
            {
                var v = slotOffVoteList.Votes.FirstOrDefault(m => m.Value > Fixed8.One * 50000000);
                ok = default(KeyValuePair<uint, Fixed8>).Equals(v);
            }
            return ok;
        }
        public static IEnumerable<AccountState> GetAllValidSlots(this Blockchain blockchain)
        {
            return blockchain.CurrentSnapshot.GetAllValidSlots();
        }
        public static IEnumerable<AccountState> GetAllValidSlots(this Snapshot snapshot)
        {
            foreach (var ats in snapshot.Accounts.Find().Select(m => m.Value))
            {
                if (ats.IsNotNull())
                {
                    var balance = ats.GetBalance(Blockchain.OXS);
                    if (balance >= Blockchain.BappSlotRentOXS && ats.SlotState == SlotStatus.Freeze && ats.SlotExpire >= snapshot.Height)
                        yield return ats;
                }
            }
        }
    }
}
