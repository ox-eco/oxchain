using OX.SmartContract.Enumerators;
using OX.VM;

namespace OX.SmartContract.Iterators
{
    internal interface IIterator : IEnumerator
    {
        StackItem Key();
    }
}
