using OX.Network.P2P.Payloads;
using System;
using OX.Persistence;
using System.Collections.Generic;
using System.Linq;
using OX;

namespace OX.Ledger
{
    public static class DAOHelper
    {
        public static Fixed8 GetAssetVoteValue(this Blockchain blockchain, UInt256 assetId, uint Height)
        {
            return blockchain.CurrentSnapshot.GetAssetVoteValue(assetId, Height);
        }
        public static Fixed8 GetAssetVoteValue(this Snapshot snapshot, UInt256 assetId, uint Height)
        {
            Fixed8 value = Fixed8.Zero;
            var daoVoteList = snapshot.DaoVoteList.TryGet(assetId);
            if (daoVoteList.IsNotNull())
            {
                if (daoVoteList.Votes.TryGetValue(Height, out Fixed8 v))
                    value = v;
            }
            return value;
        }
    }
}
