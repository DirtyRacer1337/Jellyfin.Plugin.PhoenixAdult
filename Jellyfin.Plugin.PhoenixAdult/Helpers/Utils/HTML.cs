using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class HTML
    {
        public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
        {
            HtmlNode html = new HtmlDocument().DocumentNode;
            var http = await HTTP.Request(url, HTTP.CreateRequest(headers, cookies), cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                html = ElementFromStream(http.ContentStream);
            }

            return html;
        }

        public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken, bool redirect)
            => await ElementFromURL(url, cancellationToken, null, null, redirect).ConfigureAwait(false);

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
}
