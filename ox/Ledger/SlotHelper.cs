using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;

namespace OX.Ledger
{
    public static class SlotHelper
    {
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 bizValidatorScriptHash)
        {
            return VerifySlotValidator(blockchain.CurrentSnapshot, bizValidatorScriptHash, out Fixed8 _, out Fixed8 _);
        }
        public static bool VerifySlotValidator(this Blockchain blockchain, UInt160 bizValidatorScriptHash, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            return VerifySlotValidator(blockchain.CurrentSnapshot, bizValidatorScriptHash, out OXSBalance, out AskFee);
        }
        public static bool VerifySlotValidator(this Snapshot snapshot, UInt160 bizValidatorScriptHash, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            OXSBalance = Fixed8.Zero;
            AskFee = Fixed8.Zero;
            var acts = snapshot.Accounts.GetAndChange(bizValidatorScriptHash, () => null);
            if (acts.IsNull()) return false;
            var balance = acts.GetBalance(Blockchain.OXS);
            OXSBalance = balance;
            AskFee = acts.AskFee;
            if (acts.SlotState == SlotStatus.UnFreeze) return false;
            if (acts.SlotExpire < snapshot.Height) return false;
            if (balance < Blockchain.BappSlotRentOXS) return false;
            return true;
        }
    }
}
