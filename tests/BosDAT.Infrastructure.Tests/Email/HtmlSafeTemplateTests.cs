using BosDAT.Infrastructure.Email;
using RazorEngineCore;

namespace BosDAT.Infrastructure.Tests.Email;

public class HtmlSafeTemplateTests
{
    private readonly IRazorEngine _engine = new RazorEngine();

    private string CompileAndRun(string templateText, object model)
    {
        var template = _engine.Compile<HtmlSafeTemplate>(templateText);
        var wrappedModel = model.IsAnonymous() ? new AnonymousTypeWrapper(model) : model;
        return template.Run(instance => instance.Model = wrappedModel);
    }

    [Fact]
    public void Write_HtmlEncodesModelProperties()
    {
        var result = CompileAndRun("Hello @Model.Name",
            new { Name = "<script>alert('xss')</script>" });

        Assert.Contains("&lt;script&gt;alert(&#x27;xss&#x27;)&lt;/script&gt;", result);
        Assert.DoesNotContain("<script>", result);
    }

    [Fact]
    public void Raw_BypassesHtmlEncoding()
    {
        var result = CompileAndRun("Hello @Raw(Model.Html)",
            new { Html = "<strong>bold</strong>" });

        Assert.Contains("<strong>bold</strong>", result);
    }

    [Fact]
    public void Write_EncodesAmpersandInText()
    {
        var result = CompileAndRun("@Model.Value",
            new { Value = "Tom & Jerry" });

        Assert.Contains("Tom &amp; Jerry", result);
    }

    [Fact]
    public void Raw_PreservesUrlWithQueryParameters()
    {
        var result = CompileAndRun(
            "<a href=\"@Raw(Model.Url)\">Link</a>",
            new { Url = "https://example.com?a=1&b=2" });

        Assert.Contains("href=\"https://example.com?a=1&b=2\"", result);
    }

    [Fact]
    public void Write_HandlesNullGracefully()
    {
        var result = CompileAndRun("Value: @Model.Value",
            new { Value = (string?)null });

        Assert.Contains("Value: ", result);
    }

    [Fact]
    public void WriteLiteral_DoesNotEncodeStaticHtml()
    {
        var result = CompileAndRun("<p>Hello <strong>@Model.Name</strong></p>",
            new { Name = "Alice" });

        Assert.Contains("<p>Hello <strong>Alice</strong></p>", result);
    }

    [Fact]
    public void Write_EncodesQuotesInAttributes()
    {
        var result = CompileAndRun(
            "<div title=\"@Model.Title\">test</div>",
            new { Title = "a\"b" });

        Assert.Contains("a&quot;b", result);
    }
}
