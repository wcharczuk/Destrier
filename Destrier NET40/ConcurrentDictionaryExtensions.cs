using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public static class ConcurrentDictionaryExtensions
    {
        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            dict.TryAdd(key, value);
        }
    }
}
