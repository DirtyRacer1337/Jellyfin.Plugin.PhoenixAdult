using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class HTML
    {
        public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken)
        {
            var http = await url.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            return html.DocumentNode;
        }

        public static HtmlNode ElementFromString(string data)
        {
            var html = new HtmlDocument();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            html.Load(stream);
            stream.Dispose();

            return html.DocumentNode;
        }

        public static HtmlNode ElementFromStream(Stream data)
        {
            var html = new HtmlDocument();
            html.Load(data);

            return html.DocumentNode;
        }
    }
}
