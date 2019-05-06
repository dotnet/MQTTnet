using System;
using System.Collections;
using System.Text;
using IronPython.Runtime;

namespace MQTTnet.Server.Scripting
{
    public static class PythonConvert
    {
        public static object ToPython(object value)
        {
            if (value is PythonDictionary)
            {
                return value;
            }

            if (value is string)
            {
                return value;
            }

            if (value is int)
            {
                return value;
            }

            if (value is float)
            {
                return value;
            }

            if (value is bool)
            {
                return value;
            }

            if (value is IDictionary dictionary)
            {
                var pythonDictionary = new PythonDictionary();
                foreach (DictionaryEntry dictionaryEntry in dictionary)
                {
                    pythonDictionary.Add(dictionaryEntry.Key, ToPython(dictionaryEntry.Value));
                }

                return pythonDictionary;
            }

            if (value is IEnumerable enumerable)
            {
                var pythonList = new List();
                foreach (var item in enumerable)
                {
                    pythonList.Add(ToPython(item));
                }

                return pythonList;
            }

            return value;
        }

        public static string Pythonfy(Enum value)
        {
            return Pythonfy(value.ToString());
        }

        public static string Pythonfy(string value)
        {
            var result = new StringBuilder();
            foreach (var @char in value)
            {
                if (char.IsUpper(@char) && result.Length > 0)
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(@char));
            }

            return result.ToString();
        }

        public static TEnum ParseEnum<TEnum>(string value) where TEnum : Enum
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value.Replace("_", string.Empty), true);
        }
    }
}
