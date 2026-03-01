---
name: email-templates
description: "RazorEngineCore email template patterns: adding new templates (model class, .cshtml file, IEmailService call), IEmailTemplateRenderer internals, testing with mocked renderer. Use when adding or modifying email templates."
---

# Email Templates

BosDAT uses **RazorEngineCore** for server-side Razor template rendering. Templates are `.cshtml` files compiled on first use and cached in-memory.

## Architecture

```
IEmailService.QueueEmailAsync(to, subject, templateName, model)
  └── saves EmailOutboxMessage to DB
        └── EmailOutboxProcessorBackgroundService (Worker)
              ├── IEmailTemplateRenderer.RenderAsync(templateName, model)  ← RazorEngineCore
              └── IEmailSender.SendAsync(to, subject, htmlBody)           ← Brevo / Console
```

| File | Purpose |
|------|---------|
| `BosDAT.Core/Interfaces/Services/IEmailTemplateRenderer.cs` | Renderer interface |
| `BosDAT.Infrastructure/Email/EmailTemplateRenderer.cs` | RazorEngineCore implementation |
| `BosDAT.Infrastructure/Email/HtmlSafeTemplate.cs` | Custom template base class (HTML-encodes output) |
| `BosDAT.Infrastructure/Email/Templates/*.cshtml` | Template files |
| `BosDAT.Core/Interfaces/Services/IEmailService.cs` | Queue entry-point |

---

## HTML Encoding

RazorEngineCore does **not** HTML-encode output by default. BosDAT uses a custom `HtmlSafeTemplate` base class that encodes all `@Model.Prop` output via `System.Text.Encodings.Web.HtmlEncoder`.

- `@Model.Prop` — HTML-encoded (safe against XSS)
- `@Raw(Model.Prop)` — bypasses encoding (use only for trusted/system-generated HTML or URLs)
- Static HTML in templates (literals) is **not** encoded — only dynamic `@Model` values are

**When to use `@Raw()`:**
- URLs in `href` attributes: `<a href="@Raw(Model.Url)">` — encoding would break `&` in query strings
- Pre-sanitized HTML content from trusted sources

**When NOT to use `@Raw()`:**
- User-supplied text (names, descriptions, comments) — always let encoding protect against XSS

---

## Adding a New Email Template

### Step 1 — Model class (Core layer)

Create a plain model class in `BosDAT.Core` (zero external dependencies):

```csharp
// BosDAT.Core/Models/Email/InvoiceEmailModel.cs
namespace BosDAT.Core.Models.Email;

public class InvoiceEmailModel
{
    public required string DisplayName { get; init; }
    public required string InvoiceNumber { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly DueDate { get; init; }
    public required string InvoiceUrl { get; init; }
}
```

### Step 2 — Cshtml template

Create `src/BosDAT.Infrastructure/Email/Templates/InvoiceEmail.cshtml`.

**Template name convention:** `PascalCase` without extension — e.g., `"InvoiceEmail"`.

The `@Model` property is dynamic; access properties directly via `@Model.Prop`:

```html
<!DOCTYPE html>
<html lang="nl">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Factuur</title>
    <style>
        body { margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f4f4f5; color: #18181b; }
        .container { max-width: 600px; margin: 0 auto; padding: 40px 20px; }
        .card { background: #ffffff; border-radius: 8px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        .header { text-align: center; margin-bottom: 24px; }
        .header h1 { font-size: 24px; font-weight: 600; margin: 0; color: #18181b; }
        .content { line-height: 1.6; color: #3f3f46; }
        .content p { margin: 0 0 16px 0; }
        .button-container { text-align: center; margin: 32px 0; }
        .button { display: inline-block; padding: 12px 32px; background-color: #18181b; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: 500; font-size: 14px; }
        .footer { text-align: center; margin-top: 24px; font-size: 12px; color: #a1a1aa; }
        .info-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #f4f4f5; font-size: 14px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="card">
            <div class="header">
                <h1>BosDAT</h1>
            </div>
            <div class="content">
                <p>Beste @Model.DisplayName,</p>
                <p>Bijgaand ontvangt u factuur <strong>@Model.InvoiceNumber</strong>.</p>

                <div class="info-row">
                    <span>Factuurnummer</span><span>@Model.InvoiceNumber</span>
                </div>
                <div class="info-row">
                    <span>Bedrag</span><span>&euro; @Model.Amount.ToString("N2")</span>
                </div>
                <div class="info-row">
                    <span>Vervaldatum</span><span>@Model.DueDate.ToString("dd-MM-yyyy")</span>
                </div>

                <div class="button-container">
                    <a href="@Raw(Model.InvoiceUrl)" class="button">Factuur bekijken</a>
                </div>
            </div>
        </div>
        <div class="footer">
            <p>Dit is een automatisch gegenereerd bericht. Reageer niet op deze e-mail.</p>
        </div>
    </div>
</body>
</html>
```

**Razor rules inside `.cshtml`:**
- `@Model.Prop` — render property (HTML-encoded via `HtmlSafeTemplate`)
- `@Raw(Model.Prop)` — render without encoding (URLs, trusted HTML only)
- `@Model.Date.ToString("dd-MM-yyyy")` — format values inline
- `@if (Model.ShowSection) { ... }` — conditionals
- `@foreach (var item in Model.Items) { ... }` — loops

### Step 3 — Copy cshtml to output directory

Add to the `.csproj` so the file is copied alongside the compiled DLL:

```xml
<!-- src/BosDAT.Infrastructure/BosDAT.Infrastructure.csproj -->
<ItemGroup>
  <Content Include="Email\Templates\InvoiceEmail.cshtml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Step 4 — Queue the email from a service

```csharp
await _emailService.QueueEmailAsync(
    to: student.Email,
    subject: $"Factuur {invoice.Number}",
    templateName: "InvoiceEmail",
    templateData: new InvoiceEmailModel
    {
        DisplayName = student.FullName,
        InvoiceNumber = invoice.Number,
        Amount = invoice.Total,
        DueDate = invoice.DueDate,
        InvoiceUrl = $"{_appSettings.FrontendBaseUrl}/invoices/{invoice.Id}"
    },
    cancellationToken);
```

---

## IEmailService Interface

```csharp
public interface IEmailService
{
    Task QueueEmailAsync(string to, string subject, string templateName,
        object templateData, CancellationToken cancellationToken = default);
}
```

- `templateName` must match a `.cshtml` filename (case-sensitive on Linux)
- `templateData` is the model object — can be anonymous or a typed class
- Calling `QueueEmailAsync` only writes to the outbox; the Worker sends asynchronously

---

## IEmailTemplateRenderer Internals

```csharp
// EmailTemplateRenderer.cs — compiled templates are cached per name
// Templates loaded from: {AppContext.BaseDirectory}/Email/Templates/{templateName}.cshtml

public Task<string> RenderAsync(string templateName, object model,
    CancellationToken cancellationToken = default);
```

- Uses `Compile<HtmlSafeTemplate>()` / `Run(Action<HtmlSafeTemplate>)` — the typed generic API
- Templates are compiled once on first access and **cached for the process lifetime**
- Thread-safe via `Lock` (.NET 9+ `System.Threading.Lock`) on the internal dictionary
- **Anonymous type wrapping:** The generic `Run(Action<T>)` does **not** auto-wrap anonymous types (unlike the non-generic `Run(object)` which does). The renderer calls `model.IsAnonymous()` and wraps with `AnonymousTypeWrapper` explicitly — both are public APIs from the RazorEngineCore library. Without this, `@Model.Prop` throws `RuntimeBinderException` because anonymous types are `internal` and invisible to the Razor-compiled assembly
- `FileNotFoundException` thrown if template file is missing at render time

### HtmlSafeTemplate design

Based on the [official example](https://github.com/adoconnection/RazorEngineCore/blob/master/ExampleAppCore/HtmlSafeTemplate.cs) with two improvements:

| Method | Behavior |
|--------|----------|
| `Write(obj)` | HTML-encodes via `HtmlEncoder.Default.Encode()` unless `obj` is `RawContent` |
| `WriteAttributeValue(...)` | Same encoding for dynamic values; skips `isLiteral` values to prevent double-encoding of author-written template HTML |
| `Raw(value)` | Wraps value in `RawContent` (preserves original `object` type) |

**Differences from official example:**
- Uses `System.Text.Encodings.Web.HtmlEncoder` instead of legacy `System.Web.HttpUtility` (modern .NET API)
- Checks `isLiteral` in `WriteAttributeValue` — official encodes everything including literals, which can double-encode `&amp;` in template source

---

## Testing

### Unit test — mock the renderer

Mock `IEmailTemplateRenderer` to avoid file system and Razor compilation:

```csharp
public class InvoiceServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _service = new InvoiceService(_uowMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task GenerateInvoice_SendsEmailWithCorrectTemplate()
    {
        // Arrange
        var student = new Student { Id = 1, Email = "alice@example.com", FirstName = "Alice" };
        _uowMock.Setup(u => u.Students.GetByIdAsync(1)).ReturnsAsync(student);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.GenerateInvoiceAsync(1);

        // Assert — verify template name and recipient
        _emailServiceMock.Verify(e => e.QueueEmailAsync(
            "alice@example.com",
            It.IsAny<string>(),
            "InvoiceEmail",                          // template name must match filename
            It.Is<object>(m => m != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration test — render real template

Use the actual `EmailTemplateRenderer` only in integration tests where the template file exists on disk:

```csharp
[Fact]
public async Task RenderAsync_InvoiceEmail_ProducesValidHtml()
{
    var renderer = new EmailTemplateRenderer(
        NullLogger<EmailTemplateRenderer>.Instance);

    var html = await renderer.RenderAsync("InvoiceEmail", new
    {
        DisplayName = "Alice",
        InvoiceNumber = "2024-001",
        Amount = 99.50m,
        DueDate = new DateOnly(2024, 12, 31),
        InvoiceUrl = "https://example.com/invoices/1"
    });

    Assert.Contains("Alice", html);
    Assert.Contains("2024-001", html);
    Assert.Contains("99,50", html);  // Dutch decimal format
}
```

> **Note:** Integration tests require the `.cshtml` file to be present in the output directory. Ensure `CopyToOutputDirectory` is set in the `.csproj` (Step 3 above).

### HtmlSafeTemplate unit tests

Tests for the encoding behavior live in `tests/BosDAT.Infrastructure.Tests/Email/HtmlSafeTemplateTests.cs`.

Key test scenarios:
- `@Model.Prop` HTML-encodes `<script>`, `&`, `"` characters
- `@Raw(Model.Prop)` bypasses encoding
- `@Raw()` preserves URLs with query parameters (`&` not encoded)
- Static HTML literals pass through unencoded
- `null` model values render as empty string

---

## Template Naming Checklist

| | Convention |
|-|------------|
| File name | `PascalCase.cshtml` (e.g. `InvoiceEmail.cshtml`) |
| Template name string | Filename without extension: `"InvoiceEmail"` |
| Model class | `PascalCaseModel` in `BosDAT.Core/Models/Email/` |
| Subject line | Passed at call site — not hardcoded in the template |
| Language | Dutch (`lang="nl"`) — unless explicitly specified |
| Styles | `<style>` block in `<head>` — supported by most modern email clients |
| URLs | Always use `@Raw()` for `href` attributes and displayed URLs |

---

## Existing Templates

| Template name | Trigger | Key model props |
|---------------|---------|-----------------|
| `InvitationEmail` | New user account created | `DisplayName`, `InvitationUrl`, `ExpiresAt` |
