using System.Dynamic;
using System.Text.Json;
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
        var wrappedModel = WrapModel(model);
        var result = compiled.Run(instance => instance.Model = wrappedModel);
        return Task.FromResult(result);
    }

    public Task<string> RenderFromContentAsync(string templateContent, string cacheKey, object model,
        CancellationToken cancellationToken = default)
    {
        var compiled = GetOrCompileFromContent(templateContent, cacheKey);
        var wrappedModel = WrapModel(model);
        var result = compiled.Run(instance => instance.Model = wrappedModel);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Wraps model objects for Razor rendering. Dictionary&lt;string,object&gt; (from JSON deserialization)
    /// becomes ExpandoObject; anonymous types use AnonymousTypeWrapper per RazorEngineCore convention.
    /// </summary>
    private static object WrapModel(object model) => model switch
    {
        Dictionary<string, object> dict => DictionaryToExpando(dict),
        _ when model.IsAnonymous() => new AnonymousTypeWrapper(model),
        _ => model
    };

    private static ExpandoObject DictionaryToExpando(Dictionary<string, object> dict)
    {
        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var kvp in dict)
        {
            expandoDict[kvp.Key] = kvp.Value is JsonElement element
                ? ConvertJsonElement(element)
                : kvp.Value;
        }
        return expando;
    }

    private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.TryGetDateTime(out var dt) ? dt : element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => DictionaryToExpando(
            JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText())!),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
        _ => element.GetRawText()
    };

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

    private IRazorEngineCompiledTemplate<HtmlSafeTemplate> GetOrCompileFromContent(
        string templateContent, string cacheKey)
    {
        lock (_lock)
        {
            if (_compiledTemplates.TryGetValue(cacheKey, out var cached))
                return cached;

            logger.LogInformation("Compiling email template from content, cacheKey: {CacheKey}", cacheKey);
            var compiled = _razorEngine.Compile<HtmlSafeTemplate>(templateContent);
            _compiledTemplates[cacheKey] = compiled;
            return compiled;
        }
    }
}
