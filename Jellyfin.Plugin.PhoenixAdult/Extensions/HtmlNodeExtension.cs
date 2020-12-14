using System;
using System.Linq;
using HtmlAgilityPack;

internal static class HtmlNodeExtension
{
    public static string SelectSingleText(this HtmlNode source, string xpath)
    {
        var res = xpath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var node = source.SelectSingleNode(xpath);

        if (node != null)
        {
            if (res.Last().StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                var attrName = res.Last().Substring(1);
                if (node.Attributes.Contains(attrName))
                {
                    return node.Attributes[attrName].Value.Trim();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                if (res.Last().Equals("text()", StringComparison.OrdinalIgnoreCase))
                {
                    return node.GetDirectInnerText().Trim();
                }
                else
                {
                    return node.InnerText.Trim();
                }
            }
        }
        else
        {
            return string.Empty;
        }
    }

    public static HtmlNodeCollection SelectNodesSafe(this HtmlNode source, string xpath)
    {
        var nodes = source.SelectNodes(xpath);

        if (nodes == null)
        {
            nodes = new HtmlNodeCollection(HtmlNode.CreateNode(string.Empty));
        }

        return nodes;
    }
}
