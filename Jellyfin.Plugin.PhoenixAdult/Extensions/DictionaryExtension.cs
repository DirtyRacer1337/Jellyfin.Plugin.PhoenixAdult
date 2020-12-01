using System.Collections.Generic;

internal static class DictionaryExtension
{
    public static Dictionary<string, string> Update(this Dictionary<string, string> source, string key, string value)
    {
        if (!source.ContainsKey(key))
        {
            source.Add(key, value);
        }
        else
        {
            source[key] = value;
        }

        return source;
    }
}
