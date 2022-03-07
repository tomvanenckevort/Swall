using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using NUglify;
using NUglify.Html;
using Swall.IO;

namespace Swall.Tasks
{
    internal class SitemapTask : SwallTask
    {
        private readonly string src;
        private readonly string[] patterns;
        private readonly string dest;
        private readonly string robots;
        private readonly string site;

        private readonly HtmlSettings htmlSettings;

        public override string Name => "sitemap";

        public SitemapTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            src = Config["src"]?.ToString();
            dest = Config["dest"]?.ToString();
            robots = Config["robots"]?.ToString();

            patterns = (Config["patterns"] as object[])?
                            .Select(c => c.ToString())?
                            .ToArray();

            site = Config["site"]?.ToString();

            htmlSettings = HtmlSettings.Pretty();
            htmlSettings.OutputTextNodesOnNewLine = false;
            htmlSettings.DecodeEntityCharacters = false;
        }

        /// <summary>
        /// Generates XML sitemap from the HTML files and pretty-prints the output XML file.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            var sitemapMatcher = new Matcher();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith('!'))
                {
                    sitemapMatcher.AddExclude(pattern.TrimStart('!'));
                }
                else
                {
                    sitemapMatcher.AddInclude(pattern);
                }
            }

            var sitemapXml = new StringBuilder();

            sitemapXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            sitemapXml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            var srcDirectory = new DirectoryInfo(src);

            var sitemapMatchResult = sitemapMatcher.Execute(new DirectoryInfoWrapper(srcDirectory));

            foreach (var match in sitemapMatchResult.Files)
            {
                var pageUrl = $"{site}/{match.Stem.Replace("index.html", string.Empty)}";

                var pageFile = new FileInfo(Path.GetFullPath(match.Path, srcDirectory.FullName));

                sitemapXml.AppendLine("<url>");

                sitemapXml.AppendLine($"<loc>{pageUrl}</loc>");

                sitemapXml.AppendLine($"<lastmod>{pageFile.LastWriteTimeUtc:yyyy-MM-ddTHH:mm:ss.fffZ}</lastmod>");

                sitemapXml.AppendLine("</url>");
            }

            sitemapXml.AppendLine("</urlset>");

            var sitemapXmlOutput = Uglify.Html(sitemapXml.ToString(), htmlSettings).Code;

            await FileAccessor.WriteAllText(dest, sitemapXmlOutput);

            if (!string.IsNullOrEmpty(robots))
            {
                WriteToConsole("Writing robots.txt");

                var robotsTxt = $"User-agent: *{Environment.NewLine}Disallow:{Environment.NewLine}{Environment.NewLine}Sitemap: {site}/sitemap.xml";

                await FileAccessor.WriteAllText(robots, robotsTxt);
            }
        }
    }
}
