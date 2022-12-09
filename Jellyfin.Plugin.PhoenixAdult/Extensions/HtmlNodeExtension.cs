using System;
using System.Linq;
using HtmlAgilityPack;

internal static class HtmlNodeExtension
{
    public static string SelectSingleText(this HtmlNode source, string xpath)
    {
        var result = string.Empty;

        var res = xpath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var node = source.SelectSingleNode(xpath);

        if (node != null)
        {
            if (res.Last().StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                var attrName = res.Last().Substring(1);
                if (node.Attributes.Contains(attrName))
                {
                    result = node.Attributes[attrName].Value.Trim();
                }
            }
            else
            {
                if (res.Last().Equals("text()", StringComparison.OrdinalIgnoreCase))
                {
                    result = node.GetDirectInnerText().Trim();
                }
                else
                {
                    result = node.InnerText.Trim();
                }
            }
        }

        return result;
    }

    public static HtmlNodeCollection SelectNodesSafe(this HtmlNode source, string xpath)
    {
        var nodes = source.SelectNodes(xpath);

        nodes ??= new HtmlNodeCollection(HtmlNode.CreateNode(string.Empty));

        return nodes;
    }
}
