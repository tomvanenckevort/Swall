using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Swall.Markdown
{
    /// <summary>
    /// Adds noopener/noreffer rel attribute to any external links.
    /// </summary>
    internal class ExternalLinksMarkdownExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed -= Pipeline_DocumentProcessed;
            pipeline.DocumentProcessed += Pipeline_DocumentProcessed;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private void Pipeline_DocumentProcessed(MarkdownDocument document)
        {
            foreach (var node in document.Descendants())
            {
                if (node is Inline)
                {
                    var link = node as LinkInline;

                    if (link?.Url?.StartsWith("http") == true)
                    {
                        link.GetAttributes().AddProperty("rel", "noopener noreferrer");
                    }
                }
            }
        }
    }
}
