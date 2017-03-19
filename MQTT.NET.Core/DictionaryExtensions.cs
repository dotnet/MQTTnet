using System;
using System.Collections.Generic;

namespace MQTTnet.Core
{
    public static class DictionaryExtensions
    {
        public static TValue Take<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
            }

            return value;
        }
    }
}
