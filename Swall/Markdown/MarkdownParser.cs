using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Swall.IO;
using Swall.Yaml;

namespace Swall.Markdown
{
    internal sealed class MarkdownParser
    {
        private readonly MarkdownPipeline pipeline;

        public MarkdownParser()
        {
            pipeline = new MarkdownPipelineBuilder()
                                .UseYamlFrontMatter()
                                .Use<ExternalLinksMarkdownExtension>()
                                .Build();
        }

        /// <summary>
        /// Parses Markdown file at specified path and returns dictionary of frontmatter, content and directory path.
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <returns></returns>
        public async Task<IReadOnlyDictionary<string, object>> Parse(string inputFilePath)
        {
            var input = await FileAccessor.ReadAllText(inputFilePath);

            var markdownDoc = Markdig.Markdown.Parse(input, pipeline);

            var frontMatterBlock = markdownDoc.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

            if (frontMatterBlock == null)
            {
                return null;
            }

            var frontMatterYaml = input.Substring(frontMatterBlock.Span.Start, frontMatterBlock.Span.Length).Trim('-');

            var frontMatter = YamlDeserializer.Deserialize(frontMatterYaml);

            var content = markdownDoc.ToHtml(pipeline);

            frontMatter.Add("content", content);

            frontMatter.Add("directoryPath", Path.GetDirectoryName(inputFilePath));

            return frontMatter;
        }
    }
}
