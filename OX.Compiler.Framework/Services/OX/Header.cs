namespace OX.SmartContract.Framework.Services
{
    public class Header : IScriptContainer
    {
        public extern byte[] Hash
        {
            [Syscall("OX.Header.GetHash")]
            get;
        }

        public extern uint Version
        {
            [Syscall("OX.Header.GetVersion")]
            get;
        }

        public extern byte[] PrevHash
        {
            [Syscall("OX.Header.GetPrevHash")]
            get;
        }

        public extern byte[] MerkleRoot
        {
            [Syscall("OX.Header.GetMerkleRoot")]
            get;
        }

        public extern uint Timestamp
        {
            [Syscall("OX.Header.GetTimestamp")]
            get;
        }

        public extern uint Index
        {
            [Syscall("OX.Header.GetIndex")]
            get;
        }

        public extern ulong ConsensusData
        {
            [Syscall("OX.Header.GetConsensusData")]
            get;
        }

        public extern byte[] NextConsensus
        {
            [Syscall("OX.Header.GetNextConsensus")]
            get;
        }
    }
}
