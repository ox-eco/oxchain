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
    public interface IBappPort
    {
        Bapp Bapp { get; set; }
    }
}
