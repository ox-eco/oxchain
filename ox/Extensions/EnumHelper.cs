using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;

namespace OX
{
    
    public static class EnumHelper
    {
        public static int Value<EnumType>(this EnumType enu) where EnumType : struct
        {
            return Convert.ToInt32(enu);
        }
      
        public static EnumType[] Parse<EnumType>(this EnumType enu) where EnumType : struct
        {
            List<EnumType> list = new List<EnumType>();
            EnumType[] ets = _Parse(enu);
            foreach (EnumType et in ets)
            {
                if (_Parse(et).Length == 1)
                    list.Add(et);
            }
            return list.ToArray();

        }
        static EnumType[] _Parse<EnumType>(EnumType enu) where EnumType : struct
        {
            string[] names = Enum.GetNames(typeof(EnumType));
            List<EnumType> list = new List<EnumType>();
            foreach (string name in names)
            {
                EnumType type = (EnumType)Enum.Parse(typeof(EnumType), name);
                if ((Convert.ToInt32(type) | Convert.ToInt32(enu)) == Convert.ToInt32(enu))
                    list.Add(type);
            }
            return list.ToArray();

        }
        public static int[] ParseValues(Type type, int value)
        {
            string[] names = Enum.GetNames(type);
            List<int> list = new List<int>();
            foreach (string name in names)
            {
                object oj = Enum.Parse(type, name);
                int v = Convert.ToInt32(oj);
                if ((v | value) == value)
                    list.Add(v);
            }
            return list.ToArray();

        }
      
        public static EnumType[] Parse<EnumType>(this int MergeValue) where EnumType : struct
        {
            EnumType hrt = (EnumType)Enum.Parse(typeof(EnumType), MergeValue.ToString());
            return Parse<EnumType>(hrt);
        }
       
        public static EnumType MergeParse<EnumType>(this int MergeValue) where EnumType : struct
        {
            EnumType hrt = (EnumType)Enum.Parse(typeof(EnumType), MergeValue.ToString());
            return hrt;
        }
        public static EnumType Total<EnumType>() where EnumType : struct
        {
            string[] names = Enum.GetNames(typeof(EnumType));
            List<EnumType> list = new List<EnumType>();
            int x = 0;
            foreach (string name in names)
            {
                EnumType type = (EnumType)Enum.Parse(typeof(EnumType), name);
                x = x | Convert.ToInt32(type);
            }
            return x.MergeParse<EnumType>();
        }
        public static EnumType[] All<EnumType>() where EnumType : struct
        {
            return EnumHelper.Total<EnumType>().Parse();
        }
    
        public static bool Contains<EnumType>(this EnumType enu, EnumType enu2) where EnumType : struct
        {
            return Convert.ToInt32(enu.intersect(enu2)) == Convert.ToInt32(enu2);
        }
        public static bool Contains(this int enu, int enu2)
        {
            return enu.intersect(enu2) == enu2;
        }
       
        public static EnumType intersect<EnumType>(this EnumType enu, EnumType enu2) where EnumType : struct
        {
            return (Convert.ToInt32(enu) & Convert.ToInt32(enu2)).MergeParse<EnumType>();
        }
        public static int Intersect(this int enu, int enu2)
        {
            return enu & enu2;
        }
       
        public static String StringMaxValue<EnumType>(this EnumType value) where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumStringsAttribute).StringsLookup;
                byte[] enums = Parse(value) as byte[];
                enums = enums.OrderBy(va => va).ToArray();
                string ret = "";
                if (StringsLookup.TryGetValue(enums[enums.Length - 1], out ret))
                {
                    return ret;
                }
            }
            return value.ToString();
        }
        public static String EngStringMaxValue<EnumType>(this EnumType value) where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumEngStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumEngStringsAttribute).StringsLookup;
                byte[] enums = Parse(value) as byte[];
                enums = enums.OrderBy(va => va).ToArray();
                string ret = "";
                if (StringsLookup.TryGetValue(enums[enums.Length - 1], out ret))
                {
                    return ret;
                }
            }
            return value.ToString();
        }
        public static string EnumName(Type type)
        {
            Attribute[] attributes = type.GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            EnumStringsAttribute EnumStringsAttribute = attributes[0] as EnumStringsAttribute;
            string name = EnumStringsAttribute.EnumName;
            return name.IsNullOrEmpty() ? string.Empty : name;
        }
        public static string EnumEngName(Type type)
        {
            Attribute[] attributes = type.GetTypeInfo().GetCustomAttributes(typeof(EnumEngStringsAttribute), false) as Attribute[];
            EnumEngStringsAttribute EnumStringsAttribute = attributes[0] as EnumEngStringsAttribute;
            string name = EnumStringsAttribute.EnumName;
            return name.IsNullOrEmpty() ? string.Empty : name;
        }
        public static string EnumName<EnumType>() where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            EnumStringsAttribute EnumStringsAttribute = attributes[0] as EnumStringsAttribute;
            string name = EnumStringsAttribute.EnumName;
            return name.IsNullOrEmpty() ? string.Empty : name;
        }
        public static string EnumEngName<EnumType>() where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumEngStringsAttribute), false) as Attribute[];
            EnumEngStringsAttribute EnumStringsAttribute = attributes[0] as EnumEngStringsAttribute;
            string name = EnumStringsAttribute.EnumName;
            return name.IsNullOrEmpty() ? string.Empty : name;
        }
       
        public static String StringValue<EnumType>(this EnumType value) where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumStringsAttribute).StringsLookup;
                return GetStringValue<EnumType>(value, StringsLookup);
            }
            return value.ToString();
        }
        public static String EngStringValue<EnumType>(this EnumType value) where EnumType : struct
        {
            Attribute[] attributes = typeof(EnumType).GetTypeInfo().GetCustomAttributes(typeof(EnumEngStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumEngStringsAttribute).StringsLookup;
                return GetStringValue<EnumType>(value, StringsLookup);
            }
            return value.ToString();
        }
        public static ICollection<KeyValuePair<byte, string>> AllStringValue(Type type)
        {
            Attribute[] attributes = type.GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumStringsAttribute).StringsLookup;
                return StringsLookup;
            }
            return new Dictionary<byte, string>();
        }
        public static ICollection<KeyValuePair<byte, string>> AllEngStringValue(Type type)
        {
            Attribute[] attributes = type.GetTypeInfo().GetCustomAttributes(typeof(EnumEngStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumEngStringsAttribute).StringsLookup;
                return StringsLookup;
            }
            return new Dictionary<byte, string>();
        }
        public static ICollection<KeyValuePair<byte, string>> StringValue(Type type, byte value)
        {
            Attribute[] attributes = type.GetTypeInfo().GetCustomAttributes(typeof(EnumStringsAttribute), false) as Attribute[];
            if (!ReferenceEquals(attributes, null) && attributes.Length > 0)
            {
                Dictionary<byte, string> StringsLookup = (attributes[0] as EnumStringsAttribute).StringsLookup;
                return GetStringValue(type, value, StringsLookup);
            }
            return new Dictionary<byte, string>();
        }
        static ICollection<KeyValuePair<byte, string>> GetStringValue(Type type, byte value, Dictionary<byte, string> StringsLookup)
        {
            Dictionary<byte, string> ret = new Dictionary<byte, string>();
            string oret;
            if (StringsLookup.TryGetValue(value, out oret))
            {
                ret.Add(value, oret);
                return ret;
            }
            int v = (int)value;
            int[] enums = v.ToFlags();
            foreach (int et in enums)
            {
                foreach (KeyValuePair<byte, string> pair in GetStringValue(type, (byte)et, StringsLookup))
                {
                    ret.Add(pair.Key, pair.Value);
                }
            }
            return ret;
        }

        static string GetStringValue<EnumType>(EnumType value, Dictionary<byte, string> StringsLookup) where EnumType : struct
        {
            byte enumValue = (value as IConvertible).ToByte(CultureInfo.InvariantCulture);
            string ret = string.Empty;
            if (StringsLookup.TryGetValue(enumValue, out ret))
            {
                return ret;
            }
            EnumType[] enums = Parse(value);
            List<string> ls = new List<string>();
            foreach (EnumType et in enums)
            {
                ls.Add(GetStringValue(et, StringsLookup));
            }
            return string.Join(",", ls.ToArray());
        }
    }
}
