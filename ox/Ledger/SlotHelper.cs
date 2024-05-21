using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;

namespace OX.Ledger
{
    public static class SlotHelper
    {
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 slotScriptHash)
        {
            return VerifySlotValidator(blockchain.CurrentSnapshot, slotScriptHash, out AccountState _, out Fixed8 _, out Fixed8 _);
        }
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 slotScriptHash, out AccountState accountState, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            return VerifySlotValidator(blockchain.CurrentSnapshot, slotScriptHash, out accountState, out OXSBalance, out AskFee);
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
    }
}
