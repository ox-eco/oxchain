using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OX
{
    [global::System.AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    [ComVisible(true)]
    public class EnumEngStringsAttribute : Attribute
    {
        public string EnumName { get; set; }
        protected Type enumType;
        private static Dictionary<string, Dictionary<byte, string>> stringsLookup = new Dictionary<string, Dictionary<byte, string>>();
        public Dictionary<byte, string> StringsLookup
        {
            get { return stringsLookup[enumType.FullName]; }
        }

        public EnumEngStringsAttribute(Type enumType, params string[] strings)
        {
            this.enumType = enumType;
            ProcessStrings(strings);
        }

        private void ProcessStrings(string[] strings)
        {
            lock (stringsLookup)
            {
                string typeName = enumType.FullName;
                if (!stringsLookup.ContainsKey(typeName))
                {
                    stringsLookup.Add(typeName, new Dictionary<byte, string>());

                    byte[] values = Enum.GetValues(enumType) as byte[];
                    if (values.Length != strings.Length)
                        throw new ArgumentException("The number of enum values differ from the number of given string values");

                    for (int index = 0; index < values.Length; index++)
                        stringsLookup[typeName].Add(values[index], strings[index]);
                }
            }
        }
    }
    [global::System.AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    [ComVisible(true)]
    public class EnumStringsAttribute : Attribute
    {
        public string EnumName { get; set; }
        protected Type enumType;
        private static Dictionary<string, Dictionary<byte, string>> stringsLookup = new Dictionary<string, Dictionary<byte, string>>();
        public Dictionary<byte, string> StringsLookup
        {
            get { return stringsLookup[enumType.FullName]; }
        }

        public EnumStringsAttribute(Type enumType, params string[] strings)
        {
            this.enumType = enumType;
            ProcessStrings(strings);
        }

        private void ProcessStrings(string[] strings)
        {
            lock (stringsLookup)
            {
                string typeName = enumType.FullName;
                if (!stringsLookup.ContainsKey(typeName))
                {
                    stringsLookup.Add(typeName, new Dictionary<byte, string>());

                    byte[] values = Enum.GetValues(enumType) as byte[];
                    if (values.Length != strings.Length)
                        throw new ArgumentException("The number of enum values differ from the number of given string values");

                    for (int index = 0; index < values.Length; index++)
                        stringsLookup[typeName].Add(values[index], strings[index]);
                }
            }
        }
    }
}
