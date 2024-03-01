using Akka.Util.Internal;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OX.Ledger
{
    public class FlashStatePool 
    {

        private readonly OXSystem _system;
         
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public FlashStatePool(OXSystem system)
        {
            _system = system;
        }


    }
}
