namespace BosDAT.Core.Constants;

public static class EmailOutboxConstants
{
    /// <summary>
    /// Sentinel template name used when the HTML body is pre-rendered and stored directly.
    /// The outbox processor skips Razor rendering and uses the stored HTML as-is.
    /// </summary>
    public const string RenderedTemplateName = "__rendered__";
}
