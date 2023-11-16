using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HandlebarsDotNet;
using HandlebarsDotNet.PathStructure;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Swall.Assets;
using Swall.IO;
using Swall.Markdown;

namespace Swall.Handlebars
{
    internal sealed class HandlebarsRenderer
    {
        private readonly DirectoryInfo templatesSrcDirectory;
        private readonly DirectoryInfo assetsSrcDirectory;
        private readonly IHandlebars handlebars;
        private readonly MarkdownParser markdownParser;

        public HandlebarsRenderer(DirectoryInfo templatesSrcDirectory, DirectoryInfo assetsSrcDirectory)
        {
            this.templatesSrcDirectory = templatesSrcDirectory;
            this.assetsSrcDirectory = assetsSrcDirectory;

            handlebars = HandlebarsDotNet.Handlebars.Create();

            markdownParser = new MarkdownParser();

            RegisterHelpers();
        }

        /// <summary>
        /// Registers Handlebars helper functions.
        /// </summary>
        private void RegisterHelpers()
        {
            handlebars.RegisterHelper("concat", (writer, context, arguments) =>
            {
                if (arguments.Length == 0)
                {
                    return;
                }

                var concatenatedArguments = string.Join(string.Empty, arguments.Select(a => a.ToString()));

                writer.Write(concatenatedArguments);
            });

            handlebars.RegisterHelper("array", (in HelperOptions options, in Context context, in Arguments arguments) =>
            {
                if (arguments.Length == 0)
                {
                    return Array.Empty<string>();
                }

                return arguments.Select(a => a.ToString()).ToArray();
            });

            handlebars.RegisterHelper("assetLink", (writer, context, arguments) =>
            {
                if (arguments.Length == 0)
                {
                    return;
                }

                var filePath = arguments[0].ToString();

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileExtension = Path.GetExtension(filePath);

                var assetMatcher = new Matcher();
                assetMatcher.AddInclude($"**/{fileName}*{fileExtension}");

                var assetMatchResults = assetMatcher.Execute(new DirectoryInfoWrapper(assetsSrcDirectory));

                if (!assetMatchResults.HasMatches)
                {
                    return;
                }

                var revisionFilePath = assetMatchResults.Files.First().Path;
                var revisionFileName = Path.GetFileNameWithoutExtension(revisionFilePath);

                var assetPath = filePath.Replace(fileName, revisionFileName);

                var integrity = HashGenerator.GenerateIntegrityHash(Path.GetFullPath(revisionFilePath, assetsSrcDirectory.FullName)).Result;

                var additionalAttributes = string.Empty;

                if (arguments.Length > 1)
                {
                    additionalAttributes = $" {arguments[1]}";
                }

                switch (fileExtension)
                {
                    case ".css":
                        writer.WriteSafeString($"<link href=\"{assetPath}\" integrity=\"{integrity}\" crossorigin=\"anonymous\" rel=\"stylesheet\"{(additionalAttributes)} />");
                        break;
                    case ".js":
                        writer.WriteSafeString($"<script src=\"{assetPath}\" integrity=\"{integrity}\" crossorigin=\"anonymous\"{(additionalAttributes)}></script>");
                        break;
                }
            });

            handlebars.RegisterHelper("now", (writer, context, arguments) =>
            {
                writer.WriteSafeString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            });

            handlebars.RegisterHelper("displayDate", (writer, context, arguments) =>
            {
                if (arguments.Length == 0)
                {
                    return;
                }

                if (DateTime.TryParse(arguments[0].ToString(), CultureInfo.InvariantCulture, out var date))
                {
                    writer.WriteSafeString(date.ToString("dd MMMM yyyy"));
                }
            });

            handlebars.RegisterHelper("childPages", (in HelperOptions options, in Context context, in Arguments arguments) =>
            {
                var hash = arguments[0] as Dictionary<string, object>;

                //find all matching child pages
                var pageDirectoryPath = options.Data.Value<Dictionary<string, object>>(ChainSegment.Root)["directoryPath"]?.ToString();
                var pageTemplates = (hash.TryGetValue("pageTemplates", out var pageTemplatesObj) ? pageTemplatesObj as string[] : null);

                var pageMatcher = new Matcher();
                pageMatcher.AddInclude($"**/*.md");

                var pages = new List<Dictionary<string, object>>();

                var pageMatchResults = pageMatcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(pageDirectoryPath)));

                foreach (var pageMatch in pageMatchResults.Files)
                {
                    var pageFile = new FileInfo(Path.GetFullPath(pageMatch.Path, pageDirectoryPath));

                    var frontMatter = markdownParser.Parse(pageFile.FullName).Result;

                    if (frontMatter == null)
                    {
                        continue;
                    }

                    if (pageTemplates?.Contains(frontMatter["template"]) == true)
                    {
                        var pageUrl = Path.GetRelativePath(pageDirectoryPath, pageFile.DirectoryName)
                                                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar;

                        var page = new Dictionary<string, object>()
                        {
                            { "url", pageUrl }
                        };

                        pages.Add(page);

                        if (hash.TryGetValue("pageFields", out var pageFieldsObj) && pageFieldsObj is string[] pageFields)
                        {
                            foreach (var pageField in pageFields)
                            {
                                if (frontMatter.TryGetValue(pageField, out var pageFieldValue))
                                {
                                    page.Add(pageField, pageFieldValue);
                                }
                            }
                        }
                    }
                }

                if (hash.TryGetValue("sort", out var sortObj) && sortObj is string sort)
                {
                    if (hash.TryGetValue("sortDirection", out var sortDirectionObj) && sortDirectionObj?.ToString() == "desc")
                    {
                        pages = pages.OrderByDescending(p => p[sort]).ToList();
                    }
                    else
                    {
                        pages = pages.OrderBy(p => p[sort]).ToList();
                    }
                }

                return pages;
            });
        }

        /// <summary>
        /// Registers all Handlebars partial view templates.
        /// </summary>
        private async Task RegisterPartials()
        {
            var partialMatcher = new Matcher();
            partialMatcher.AddInclude("_*.hbs");
            partialMatcher.AddInclude("partials/**/*.hbs");

            var partialMatchResults = partialMatcher.Execute(new DirectoryInfoWrapper(templatesSrcDirectory));

            if (!partialMatchResults.HasMatches)
            {
                return;
            }

            foreach (var path in partialMatchResults.Files.Select(m => m.Path))
            {
                var file = new FileInfo(Path.GetFullPath(path, templatesSrcDirectory.FullName));

                var partialPath = path
                                    .Replace("partials/", string.Empty)
                                    .Replace(file.Extension, string.Empty);

                var input = await FileAccessor.ReadAllText(file.FullName);

                handlebars.RegisterTemplate(partialPath, input);
            }
        }

        /// <summary>
        /// Compiles Handlebars view from the specified template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private async Task<HandlebarsTemplate<object, object>> CompileView(string template)
        {
            var templateMatcher = new Matcher();
            templateMatcher.AddInclude($"**/{template}.hbs");

            var templateMatchResults = templateMatcher.Execute(new DirectoryInfoWrapper(templatesSrcDirectory));

            if (!templateMatchResults.HasMatches)
            {
                return null;
            }

            var file = new FileInfo(Path.GetFullPath(templateMatchResults.Files.First().Path, templatesSrcDirectory.FullName));

            var input = await FileAccessor.ReadAllText(file.FullName);

            return handlebars.Compile(input);
        }

        /// <summary>
        /// Renders Handlebars view to HTML string using the specified template and provided frontmatter.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="frontMatter"></param>
        /// <returns></returns>
        public async Task<string> RenderViewToStringAsync(string template, IReadOnlyDictionary<string, object> frontMatter)
        {
            await RegisterPartials();

            var view = await CompileView(template);

            return view(frontMatter);
        }
    }
}
