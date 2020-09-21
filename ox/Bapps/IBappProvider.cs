using System;
using OX.Persistence;
using System.Collections.Generic;
using static OX.Ledger.Blockchain;
using OX.Network.P2P.Payloads;
using System.Security.Permissions;
using OX.Wallets;
using OX.Bapps;

namespace OX.Bapps
{
    public interface IBappProvider : IBappPort
    {
     
        Wallet Wallet { get; set; }
        void OnBlock(Block block);
        void OnRebuild(Wallet wallet = null);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
    }

}
