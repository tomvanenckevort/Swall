using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using NUglify;
using NUglify.Html;
using Swall.Handlebars;
using Swall.IO;
using Swall.Markdown;

namespace Swall.Tasks
{
    internal sealed class HtmlTask : SwallTask
    {
        private readonly string contentSrc;
        private readonly string templatesSrc;
        private readonly string assetsSrc;
        private readonly string dest;

        private readonly MarkdownParser markdownParser;

        private readonly HtmlSettings htmlSettings;

        public override string Name => "html";

        public HtmlTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            var src = Config["src"] as IReadOnlyDictionary<string, object>;

            contentSrc = src?["content"]?.ToString() ?? "\\";
            templatesSrc = src?["templates"]?.ToString() ?? "\\";
            assetsSrc = src?["assets"]?.ToString() ?? "\\";
            dest = Config["dest"]?.ToString() ?? "\\";

            markdownParser = new MarkdownParser();

            htmlSettings = HtmlSettings.Pretty();
            htmlSettings.OutputTextNodesOnNewLine = false;
            htmlSettings.DecodeEntityCharacters = false;
            htmlSettings.InlineTagsPreservingSpacesAround.Remove("a");
            htmlSettings.InlineTagsPreservingSpacesAround.Remove("time");
        }

        /// <summary>
        /// Finds all Markdown files and compiles them to HTML using the provided Handlebars templates.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            var contentSrcDirectory = new DirectoryInfo(contentSrc);

            if (!contentSrcDirectory.Exists)
            {
                WriteToConsole("Content source directory missing", ConsoleColor.Red);
                return;
            }

            var templatesSrcDirectory = new DirectoryInfo(templatesSrc);

            if (!templatesSrcDirectory.Exists)
            {
                WriteToConsole("Templates source directory missing", ConsoleColor.Red);
                return;
            }

            var assetsSrcDirectory = new DirectoryInfo(assetsSrc);

            if (!assetsSrcDirectory.Exists)
            {
                WriteToConsole("Assets source directory missing", ConsoleColor.Red);
                return;
            }

            var destDirectory = new DirectoryInfo(dest);

            if (!destDirectory.Exists)
            {
                Directory.CreateDirectory(destDirectory.FullName);
            }

            var contentMatcher = new Matcher();
            contentMatcher.AddInclude("**/*.md");

            var contentMatchResult = contentMatcher.Execute(new DirectoryInfoWrapper(contentSrcDirectory));

            if (!contentMatchResult.HasMatches)
            {
                WriteToConsole("No content Markdown files found", ConsoleColor.Yellow);
                return;
            }

            var handlebarsRenderer = new HandlebarsRenderer(templatesSrcDirectory, assetsSrcDirectory);

            foreach (var match in contentMatchResult.Files)
            {
                WriteToConsole($"Processing {match.Stem}");

                var file = new FileInfo(Path.GetFullPath(match.Path, contentSrcDirectory.FullName));

                var inputFilePath = file.FullName;
                var inputFileDirectoryPath = Path.GetRelativePath(contentSrcDirectory.FullName, file.DirectoryName);
                var outputFileDirectoryPath = Path.GetFullPath(inputFileDirectoryPath, destDirectory.FullName);
                var outputFilePath = Path.GetFullPath(file.Name.Replace(file.Extension, $".html"), outputFileDirectoryPath);

                if (outputFilePath.Contains(".xml.html"))
                {
                    outputFilePath = outputFilePath.Replace(".xml.html", ".xml");
                }

                var frontMatter = await markdownParser.Parse(inputFilePath);

                if (frontMatter == null)
                {
                    WriteToConsole($"Missing front matter", ConsoleColor.Yellow);

                    continue;
                }

                var template = frontMatter["template"]?.ToString();

                var html = await handlebarsRenderer.RenderViewToStringAsync(template, frontMatter);

                html = Uglify.Html(html, htmlSettings).Code;

                var outputFileDirectory = new DirectoryInfo(outputFileDirectoryPath);

                if (!outputFileDirectory.Exists)
                {
                    Directory.CreateDirectory(outputFileDirectory.FullName);
                }

                await FileAccessor.WriteAllText(outputFilePath, html);
            }
        }
    }
}
