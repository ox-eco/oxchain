namespace OX.SmartContract.Framework.Services
{
    public static class Runtime
    {
        public static extern TriggerType Trigger
        {
            [Syscall("OX.Runtime.GetTrigger")]
            get;
        }

        public static extern uint Time
        {
            [Syscall("OX.Runtime.GetTime")]
            get;
        }

        [Syscall("OX.Runtime.CheckWitness")]
        public static extern bool CheckWitness(byte[] hashOrPubkey);

        [Syscall("OX.Runtime.Notify")]
        public static extern void Notify(params object[] state);

        [Syscall("OX.Runtime.Log")]
        public static extern void Log(string message);
    }
}
