namespace OX.SmartContract.Framework.Services
{
    public class Iterator<TKey, TValue>
    {
        [Syscall("OX.Iterator.Next")]
        public extern bool Next();

        public extern TKey Key
        {
            [Syscall("OX.Iterator.Key")]
            get;
        }

        public extern TValue Value
        {
            [Syscall("OX.Iterator.Value")]
            get;
        }
    }
}
