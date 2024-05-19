using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;

namespace OX.Ledger
{
    public static class FlashMessageVerifyHelper
    {
        public static bool VerifyFlashMessageSender(this Blockchain blockchain, UInt160 flashMessageSender, out Fixed8 OXSBalance)
        {
            OXSBalance = Fixed8.Zero;
            var valid = blockchain.CurrentSnapshot.VerifyFlashMessageSender(flashMessageSender, out AccountState accountState);
            OXSBalance = accountState.GetBalance(Blockchain.OXC);
            return valid;
        }
        public static bool VerifyFlashMessageSender(this Snapshot snapshot, UInt160 flashMessageSender, out AccountState accountState)
        {
            accountState = snapshot.Accounts.GetAndChange(flashMessageSender, () => null);
            if (accountState.IsNull()) return false;
            if (accountState.GetBalance(Blockchain.OXC) < Blockchain.FlashMinOXCBalance) return false;
            return true;
        }
    }
}
