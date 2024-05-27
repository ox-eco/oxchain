using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;
using System.Collections.Generic;
using System.Linq;

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
        public static IEnumerable<AccountState> GetAllValidSlots(this Blockchain blockchain)
        {
            return blockchain.CurrentSnapshot.GetAllValidSlots();
        }
        public static IEnumerable<AccountState> GetAllValidSlots(this Snapshot snapshot)
        {
            foreach (var ats in snapshot.Accounts.Find().Select(m => m.Value))
            {
                var balance = ats.GetBalance(Blockchain.OXS);
                if (balance >= Blockchain.BappSlotRentOXS && ats.SlotState == SlotStatus.Freeze && ats.SlotExpire >= snapshot.Height)
                    yield return ats;
            }
        }
    }
}
