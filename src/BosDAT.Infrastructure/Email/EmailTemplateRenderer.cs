using BosDAT.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using RazorEngineCore;

namespace BosDAT.Infrastructure.Email;

public class EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger) : IEmailTemplateRenderer
{
    private readonly IRazorEngine _razorEngine = new RazorEngine();
    private readonly Dictionary<string, IRazorEngineCompiledTemplate<HtmlSafeTemplate>> _compiledTemplates = new();
    private readonly Lock _lock = new();
    private readonly string _templateBasePath = Path.Combine(
        AppContext.BaseDirectory, "Email", "Templates");

    public Task<string> RenderAsync(string templateName, object model,
        CancellationToken cancellationToken = default)
    {
        var compiled = GetOrCompileTemplate(templateName);
        // The generic Run(Action<T>) does not wrap anonymous types automatically
        // (unlike the non-generic Run(object) which does). Use the library's own
        // AnonymousTypeWrapper to handle nested anonymous types, collections, and dictionaries.
        var wrappedModel = model.IsAnonymous() ? new AnonymousTypeWrapper(model) : model;
        var result = compiled.Run(instance => instance.Model = wrappedModel);
        return Task.FromResult(result);
    }

    private IRazorEngineCompiledTemplate<HtmlSafeTemplate> GetOrCompileTemplate(string templateName)
    {
        lock (_lock)
        {
            if (_compiledTemplates.TryGetValue(templateName, out var cached))
                return cached;

            var templatePath = Path.Combine(_templateBasePath, $"{templateName}.cshtml");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template '{templateName}' not found at {templatePath}");

            var templateContent = File.ReadAllText(templatePath);
            logger.LogInformation("Compiling email template: {TemplateName}", templateName);

            var compiled = _razorEngine.Compile<HtmlSafeTemplate>(templateContent);
            _compiledTemplates[templateName] = compiled;
            return compiled;
        }
    }
}
