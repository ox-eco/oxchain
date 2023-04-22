namespace OX.Network.P2P.Payloads
{
    public enum TransactionAttributeUsage : byte
    {
        ContractHash = 0x00,

        ECDH02 = 0x02,
        ECDH03 = 0x03,

        Script = 0x20,

        Vote = 0x30,

        DescriptionUrl = 0x81,
        Description = 0x90,

        Hash1 = 0xa1,
        Hash2 = 0xa2,
        Hash3 = 0xa3,
        Hash4 = 0xa4,
        Hash5 = 0xa5,
        Hash6 = 0xa6,
        Hash7 = 0xa7,
        Hash8 = 0xa8,
        Hash9 = 0xa9,
        Hash10 = 0xaa,
        Hash11 = 0xab,
        Hash12 = 0xac,
        Hash13 = 0xad,
        Hash14 = 0xae,
        Hash15 = 0xaf,

        Remark = 0xf0,
        Remark1 = 0xf1,
        Remark2 = 0xf2,
        Remark3 = 0xf3,
        Remark4 = 0xf4,
        AgentTip = 0xf5,
        Tip1 = 0xf6,
        Tip2 = 0xf7,
        Tip3 = 0xf8,
        Tip4 = 0xf9,
        Tip5 = 0xfa,
        Tip6 = 0xfb,
        RelatedData = 0xfc,
        EthSignature = 0xfd,
        RelatedScriptHash = 0xfe,
        RelatedPublicKey = 0xff
    }
}
