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
        // When retrieved from the outbox, the model arrives as Dictionary<string,object> with JsonElement
        // values. Convert to ExpandoObject so Razor can access properties via @Model.PropertyName.
        // Anonymous types (passed directly) are wrapped via AnonymousTypeWrapper per RazorEngineCore convention.
        var wrappedModel = model switch
        {
            Dictionary<string, object> dict => DictionaryToExpando(dict),
            _ when model.IsAnonymous() => (object)new AnonymousTypeWrapper(model),
            _ => model
        };
        var result = compiled.Run(instance => instance.Model = wrappedModel);
        return Task.FromResult(result);
    }

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
}
