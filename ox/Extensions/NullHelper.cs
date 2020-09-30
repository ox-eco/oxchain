using System.Collections.Generic;
using System.Linq;

namespace OX
{
    public static class NullHelper
    {
        public static bool IsNullOrEmpty<T>(this T[] collection)
        {
            return collection == default(T[]) || collection.Length < 1;
        }
        public static bool IsNotNullAndEmpty<T>(this T[] collection)
        {
            return !collection.IsNullOrEmpty();
        }
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == default(IEnumerable<T>) || collection.Count() < 1;
        }
        public static bool IsNotNullAndEmpty<T>(this IEnumerable<T> collection)
        {
            return !collection.IsNullOrEmpty();
        }
        public static bool IsNull<T>(this T obj) where T : class
        {
            return obj == default(T);
        }
        public static bool IsNotNull<T>(this T obj) where T : class
        {
            return !obj.IsNull();
        }
    }
}
