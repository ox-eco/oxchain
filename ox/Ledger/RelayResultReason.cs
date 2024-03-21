namespace OX.Ledger
{
    public enum RelayResultReason : byte
    {
        Succeed,
        AlreadyExists,
        OutOfMemory,
        UnableToVerify,
        Invalid,
        PolicyFail,
        FlashInsufficientBalance,
        InFlashBlackList,
        NotInFlashWhiteList,
        Unknown
    }
}
