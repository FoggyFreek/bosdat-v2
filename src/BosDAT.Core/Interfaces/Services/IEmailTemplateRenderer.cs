namespace BosDAT.Core.Interfaces.Services;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model,
        CancellationToken cancellationToken = default);

    Task<string> RenderFromContentAsync(string templateContent, string cacheKey, object model,
        CancellationToken cancellationToken = default);
}
