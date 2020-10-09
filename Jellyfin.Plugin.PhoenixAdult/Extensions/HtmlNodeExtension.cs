using System;
using System.Linq;
using HtmlAgilityPack;

internal static class HtmlNodeExtension
{
    public static string SelectSingleText(this HtmlNode source, string xpath)
    {
        var res = xpath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (!res.Last().StartsWith("@", StringComparison.OrdinalIgnoreCase))
        {
            return source.SelectSingleNode(xpath).Attributes[res.Last()].Value.Trim();
        }
        else
        {
            if (res.Last().Equals("text()", StringComparison.OrdinalIgnoreCase))
            {
                return source.SelectSingleNode(xpath).GetDirectInnerText().Trim();
            }
            else
            {
                return source.SelectSingleNode(xpath).InnerText.Trim();
            }
        }
    }
}
