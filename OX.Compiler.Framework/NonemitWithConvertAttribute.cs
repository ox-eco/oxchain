using System;

namespace OX.SmartContract.Framework
{
    public enum ConvertMethod
    {
        HexToBytes,
        ToScriptHash,
        ToBigInteger,
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
    public class NonemitWithConvertAttribute : Attribute
    {
        public ConvertMethod Method { get; }

        public NonemitWithConvertAttribute(ConvertMethod method)
        {
            this.Method = method;
        }
    }
}
