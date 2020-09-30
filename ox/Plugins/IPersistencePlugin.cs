using OX.Persistence;
using System;
using System.Collections.Generic;
using static OX.Ledger.Blockchain;

namespace OX.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(Snapshot snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList);
        void OnCommit(Snapshot snapshot);
        bool ShouldThrowExceptionFromCommit(Exception ex);
    }
}
