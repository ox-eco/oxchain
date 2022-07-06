namespace OX.SmartContract.Framework.Services
{
    public class Contract
    {
        public extern byte[] Script
        {
            [Syscall("OX.Contract.GetScript")]
            get;
        }

        public extern bool IsPayable
        {
            [Syscall("OX.Contract.IsPayable")]
            get;
        }

        public extern StorageContext StorageContext
        {
            [Syscall("OX.Contract.GetStorageContext")]
            get;
        }

        [Syscall("OX.Contract.Create")]
        public static extern Contract Create(byte[] script, byte[] parameter_list, byte return_type, ContractPropertyState contract_property_state, string name, string version, string author, string email, string description);

        [Syscall("OX.Contract.Migrate")]
        public static extern Contract Migrate(byte[] script, byte[] parameter_list, byte return_type, ContractPropertyState contract_property_state, string name, string version, string author, string email, string description);

        [Syscall("OX.Contract.Destroy")]
        public static extern void Destroy();
    }
}
