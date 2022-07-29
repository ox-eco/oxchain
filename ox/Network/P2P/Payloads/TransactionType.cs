#pragma warning disable CS0612

using OX.IO.Caching;

namespace OX.Network.P2P.Payloads
{
    public enum TransactionType : byte
    {
        [ReflectionCache(typeof(MinerTransaction))]
        MinerTransaction = 0x00,
        [ReflectionCache(typeof(IssueTransaction))]
        IssueTransaction = 0x01,
        [ReflectionCache(typeof(ClaimTransaction))]
        ClaimTransaction = 0x02,
        [ReflectionCache(typeof(EnrollmentTransaction))]
        EnrollmentTransaction = 0x20,
        [ReflectionCache(typeof(RegisterTransaction))]
        RegisterTransaction = 0x40,
        [ReflectionCache(typeof(ContractTransaction))]
        ContractTransaction = 0x80,
        [ReflectionCache(typeof(StateTransaction))]
        StateTransaction = 0x90,
        /// <summary>
        /// Publish scripts to the blockchain for being invoked later.
        /// </summary>
        [ReflectionCache(typeof(PublishTransaction))]
        PublishTransaction = 0xd0,
        [ReflectionCache(typeof(InvocationTransaction))]
        InvocationTransaction = 0xd1,
        [ReflectionCache(typeof(BillTransaction))]
        BillTransaction = 0xc2,
        [ReflectionCache(typeof(CharitableTransaction))]
        CharitableTransaction = 0xc3,
        [ReflectionCache(typeof(GovementTransaction))]
        GovementTransaction = 0xc4,
        [ReflectionCache(typeof(DetainTransaction))]
        DetainTransaction = 0xc5,
        [ReflectionCache(typeof(EventTransaction))]
        EventTransaction = 0xc6,
        [ReflectionCache(typeof(ReplyTransaction))]
        ReplyTransaction = 0xc7,
        [ReflectionCache(typeof(AskTransaction))]
        AskTransaction = 0xc8,
        [ReflectionCache(typeof(TreatyTransaction))]
        TreatyTransaction = 0xc9,
        [ReflectionCache(typeof(RewardTransaction))]
        RewardTransaction = 0xca,
        [ReflectionCache(typeof(NFTCoinTransaction))]
        NFTCoinTransaction = 0xcb,
        [ReflectionCache(typeof(NFTDonateTransaction))]
        NFTDonateTransaction = 0xcc,
        [ReflectionCache(typeof(LockAssetTransaction))]
        LockAssetTransaction =0xcd,
    }
}
