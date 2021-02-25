using OX.IO;
using OX.IO.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace OX.Network.P2P.Payloads
{
    public class BizReflectionCache<T> : Dictionary<T, Type>
    {
        public BizReflectionCache() { }
        public static BizReflectionCache<T> CreateFromEnum<EnumType>() where EnumType : struct, IConvertible
        {
            Type enumType = typeof(EnumType);

            if (!enumType.GetTypeInfo().IsEnum)
                throw new ArgumentException("K must be an enumerated type");

            BizReflectionCache<T> r = new BizReflectionCache<T>();

            foreach (object t in Enum.GetValues(enumType))
            {
                MemberInfo[] memInfo = enumType.GetMember(t.ToString());
                if (memInfo == null || memInfo.Length != 1)
                    throw (new FormatException());

                ReflectionCacheAttribute attribute = memInfo[0].GetCustomAttributes(typeof(ReflectionCacheAttribute), false)
                    .Cast<ReflectionCacheAttribute>()
                    .FirstOrDefault();

                if (attribute == null)
                    throw (new FormatException());
                r.Add((T)t, attribute.Type);
            }
            return r;
        }
        public object CreateInstance(T key, object def = null)
        {
            Type tp;
            if (TryGetValue(key, out tp)) return Activator.CreateInstance(tp);
            return def;
        }
        public K CreateInstance<K>(T key, K def = default(K))
        {
            Type tp;
            if (TryGetValue(key, out tp))
                return (K)Activator.CreateInstance(tp);
            return def;
        }
        public bool GetCachedType(T key, out Type tp)
        {
            return TryGetValue(key, out tp);
        }
    }
    public class RecordHelper<T> where T : struct, IConvertible
    {
        internal static BizReflectionCache<byte> PrefixReflectionCache = BizReflectionCache<byte>.CreateFromEnum<T>();
        static RecordHelper()
        {

        }
        public static Record BuildRecord<Model>(Model recordSubModel, UInt160 scriptHash, byte[] key) where Model : BizModel, new()
        {
            return new Record()
            {
                ScriptHash = scriptHash,
                Prefix = recordSubModel.Prefix,
                Key = key,
                Data = recordSubModel.ToArray()
            };
        }
        public static BizRecordModel BuildModel(Record record)
        {
            BizRecordModel model = new BizRecordModel();
            model.ScriptHash = record.ScriptHash;
            model.Key = record.Key;
            if (PrefixReflectionCache.GetCachedType(record.Prefix, out Type tp))
            {
                var obj = record.Data.AsSerializable(tp);
                if (obj != default && obj is BizModel bm)
                    model.Model = bm;
            }
            return model;
        }
    }
    public static class SeriaHelper
    {

        public static T DeserializeFrom<T>(BinaryReader reader) where T : ISerializable, new()
        {
            T record = new T();
            record.Deserialize(reader);
            return record;
        }
        public static byte[] StringToBytes(this string str)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write(str);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
