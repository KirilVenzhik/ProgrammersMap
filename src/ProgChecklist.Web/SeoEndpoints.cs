using System.Text;
using System.Xml;
using ProgChecklist.Core;

namespace ProgChecklist.Web;

public static class SeoEndpoints
{
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>Maps /sitemap.xml and /robots.txt.</summary>
    public static WebApplication MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", (ITopicCatalog catalog, ISiteUrls urls) =>
        {
            using var stream = new MemoryStream();
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
            using (var writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("urlset", SitemapNamespace);
                foreach (var path in SitemapPaths.All(catalog))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", urls.Absolute(path));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return Results.Bytes(stream.ToArray(), "application/xml; charset=utf-8");
        });

        app.MapGet("/robots.txt", (ISiteUrls urls) =>
            Results.Text($"User-agent: *\nAllow: /\nSitemap: {urls.Absolute("/sitemap.xml")}\n", "text/plain; charset=utf-8"));

        return app;
    }
}
