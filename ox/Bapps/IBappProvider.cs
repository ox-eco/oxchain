using System;
using OX.Persistence;
using System.Collections.Generic;
using static OX.Ledger.Blockchain;
using OX.Network.P2P.Payloads;
using System.Security.Permissions;
using OX.Wallets;
using OX.Bapps;

namespace OX.BizSystems
{
    public interface IBappProvider
    {
        Bapp Bapp { get; set; }
        void OnBlock(Block block);
        void OnRebuild(Wallet wallet = null);
    }

}
