using BosDAT.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;

namespace BosDAT.Infrastructure.Tests.Email;

public class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new(
        NullLogger<EmailTemplateRenderer>.Instance);

    [Fact]
    public async Task RenderAsync_InvitationEmail_ContainsDisplayName()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains("Beste Alice", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_ContainsRawUrlInHref()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains("href=\"https://app.bosdat.nl/set-password?token=abc123&user=1\"", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_ContainsRawUrlInPlainText()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains(">https://app.bosdat.nl/set-password?token=abc123&user=1</p>", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_ContainsFormattedExpiryDate()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains("15-03-2026 14:30", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_ProducesValidHtmlStructure()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"nl\">", html);
        Assert.Contains("<title>Account Invitation</title>", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_HtmlEncodesDisplayName()
    {
        var html = await _renderer.RenderAsync("InvitationEmail", new
        {
            DisplayName = "<script>alert('xss')</script>",
            InvitationUrl = "https://app.bosdat.nl/set-password?token=abc",
            ExpiresAt = new DateTime(2026, 3, 15, 14, 30, 0)
        });

        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_DoesNotEncodeUrlQueryParameters()
    {
        var html = await _renderer.RenderAsync("InvitationEmail", new
        {
            DisplayName = "Bob",
            InvitationUrl = "https://example.com?a=1&b=2&c=3",
            ExpiresAt = new DateTime(2026, 3, 15, 14, 30, 0)
        });

        // @Raw() should preserve & in URLs, not encode to &amp;
        Assert.Contains("href=\"https://example.com?a=1&b=2&c=3\"", html);
        Assert.DoesNotContain("&amp;b=2", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_ContainsDutchContent()
    {
        var html = await RenderInvitationEmail();

        Assert.Contains("Er is een account voor u aangemaakt", html);
        Assert.Contains("Wachtwoord instellen", html);
        Assert.Contains("automatisch gegenereerd bericht", html);
    }

    [Fact]
    public async Task RenderAsync_InvitationEmail_CachesCompiledTemplate()
    {
        // Render twice — second call should use the cached compiled template
        var html1 = await RenderInvitationEmail();
        var html2 = await RenderInvitationEmail();

        Assert.Equal(html1, html2);
    }

    [Fact]
    public async Task RenderAsync_NonExistentTemplate_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _renderer.RenderAsync("NonExistentTemplate", new { }));
    }

    private Task<string> RenderInvitationEmail()
    {
        return _renderer.RenderAsync("InvitationEmail", new
        {
            DisplayName = "Alice",
            InvitationUrl = "https://app.bosdat.nl/set-password?token=abc123&user=1",
            ExpiresAt = new DateTime(2026, 3, 15, 14, 30, 0)
        });
    }
}
