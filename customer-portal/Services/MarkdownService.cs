using Markdig;

namespace Aski.Tickets.Portal.Services;

/// <summary>Converte Markdown in HTML per le note di rilascio.</summary>
public sealed class MarkdownService
{
    private readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public string ToHtml(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown) ? "" : Markdown.ToHtml(markdown, _pipeline);
}
