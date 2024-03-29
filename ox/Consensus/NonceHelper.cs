﻿using System;

namespace OX.Consensus
{
    public static class NonceHelper
    {
        public static ulong GetNonce(this IConsensusContext context)
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }
    }
}
