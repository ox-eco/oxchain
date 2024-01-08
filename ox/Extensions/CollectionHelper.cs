using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OX
{

    public static class CollectionHelper
    {
        public static IEnumerable<T> GetMoreThanOnceRepeated<T>(this IEnumerable<T> extList, Func<T, object> groupProps) where T : class
        {
            return extList
                .GroupBy(groupProps)
                .SelectMany(z => z.Skip(1));
        }
        public static IEnumerable<T> GetAllRepeated<T>(this IEnumerable<T> extList, Func<T, object> groupProps) where T : class
        {
            return extList
                .GroupBy(groupProps)
                .Where(z => z.Count() > 1)
                .SelectMany(z => z);
        }
    }
}
