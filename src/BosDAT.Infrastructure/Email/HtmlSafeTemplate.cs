using System.Text.Encodings.Web;
using RazorEngineCore;

namespace BosDAT.Infrastructure.Email;

/// <summary>
/// Template base class that HTML-encodes all output by default.
/// Use <c>@Raw(value)</c> in templates to bypass encoding for trusted HTML.
/// </summary>
public class HtmlSafeTemplate : RazorEngineTemplateBase
{
    /// <summary>
    /// Wraps a value so it bypasses HTML encoding in <see cref="Write"/>.
    /// Usage in templates: <c>@Raw(Model.SomeHtml)</c>
    /// </summary>
    public object Raw(object? value)
    {
        return new RawContent(value);
    }

    /// <summary>
    /// Overrides default Write to HTML-encode output.
    /// Values wrapped via <see cref="Raw"/> skip encoding.
    /// </summary>
    public override void Write(object? obj = null)
    {
        var value = obj is RawContent raw
            ? raw.Value
            : HtmlEncoder.Default.Encode(obj?.ToString() ?? string.Empty);

        base.Write(value);
    }

    /// <summary>
    /// Overrides attribute value writing to HTML-encode dynamic values.
    /// Literals (author-written template HTML) and <see cref="Raw"/> values skip encoding.
    /// </summary>
    public override void WriteAttributeValue(string prefix, int prefixOffset, object? value,
        int valueOffset, int valueLength, bool isLiteral)
    {
        if (!isLiteral && value is not RawContent)
            value = HtmlEncoder.Default.Encode(value?.ToString() ?? string.Empty);
        else if (value is RawContent raw)
            value = raw.Value;

        base.WriteAttributeValue(prefix, prefixOffset, value, valueOffset, valueLength, isLiteral);
    }

    private sealed class RawContent(object? value)
    {
        public object? Value { get; } = value;
    }
}
