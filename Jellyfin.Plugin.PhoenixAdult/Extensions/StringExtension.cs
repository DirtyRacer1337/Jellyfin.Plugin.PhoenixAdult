using System;
using System.Text.RegularExpressions;

internal static class StringExtension
{
    public static bool Contains(this string source, string toCheck, StringComparison stringComparison)
        => source?.IndexOf(toCheck, stringComparison) >= 0;

    public static string Replace(this string source, string from, string to, int nums, StringComparison stringComparison)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(from))
        {
            return source;
        }

        for (var i = 0; i < nums; i++)
        {
            var pos = source.IndexOf(from, stringComparison);
            if (pos < 0)
            {
                return source;
            }

            source = source.Substring(0, pos) + to + source.Substring(pos + from.Length);
        }

        return source;
    }

    public static string[] Split(this string source, string separator)
    {
        return Regex.Split(source, Regex.Escape(separator));
    }
}
