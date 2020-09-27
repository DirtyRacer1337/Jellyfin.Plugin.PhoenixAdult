using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

internal static class HTML
{
    public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
    {
        HtmlNode html = new HtmlDocument().DocumentNode;
        var http = await HTTP.Request(new HTTP.HTTPRequest
        {
            _url = url,
            _headers = headers,
            _cookies = cookies,
        }, cancellationToken).ConfigureAwait(false);
        if (http._response.IsSuccessStatusCode)
            html = ElementFromStream(await http._response.Content.ReadAsStreamAsync().ConfigureAwait(false));

        return html;
    }

    public static HtmlNode ElementFromString(string data)
    {
        HtmlNode html;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            html = ElementFromStream(stream);
        }

        return html;
    }

    public static HtmlNode ElementFromStream(Stream data)
    {
        var html = new HtmlDocument();
        html.Load(data);

        return html.DocumentNode;
    }
}
