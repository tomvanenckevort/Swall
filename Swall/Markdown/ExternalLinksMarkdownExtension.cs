using System.Linq;
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
    internal sealed class ExternalLinksMarkdownExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed -= Pipeline_DocumentProcessed;
            pipeline.DocumentProcessed += Pipeline_DocumentProcessed;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            // Constructor not used in this case
        }

        private void Pipeline_DocumentProcessed(MarkdownDocument document)
        {
            foreach (var node in document.Descendants().Where(n => n is Inline))
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
