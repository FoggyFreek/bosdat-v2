using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BosDAT.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BosDAT.Infrastructure.Email;

public class BrevoEmailSender(
    HttpClient httpClient,
    IOptions<EmailSettings> settings,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";
    private const int MaxBatchSize = 1000;

    public async Task<string> SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var emailSettings = settings.Value;

        var request = new BrevoSendRequest
        {
            Sender = new BrevoContact { Email = emailSettings.FromEmail, Name = emailSettings.FromName },
            To = [new BrevoContact { Email = to }],
            Subject = subject,
            HtmlContent = htmlBody
        };

        EnsureHeaders(emailSettings);

        var response = await httpClient.PostAsJsonAsync(ApiUrl, request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Brevo API error {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Brevo API returned {response.StatusCode}: {responseBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<BrevoSendResponse>(cancellationToken);
        var messageId = result?.MessageId ?? "unknown";

        logger.LogInformation("Email sent via Brevo to {To}, messageId: {MessageId}", to, messageId);
        return messageId;
    }

    public async Task<IReadOnlyList<string>> SendBatchAsync(IReadOnlyList<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            return [];

        if (messages.Count > MaxBatchSize)
            throw new ArgumentException($"Batch size {messages.Count} exceeds Brevo maximum of {MaxBatchSize}.");

        var emailSettings = settings.Value;

        var request = new BrevoBatchRequest
        {
            Sender = new BrevoContact { Email = emailSettings.FromEmail, Name = emailSettings.FromName },
            MessageVersions = messages.Select(m => new BrevoMessageVersion
            {
                To = [new BrevoContact { Email = m.To }],
                Subject = m.Subject,
                HtmlContent = m.HtmlBody
            }).ToList()
        };

        EnsureHeaders(emailSettings);

        var response = await httpClient.PostAsJsonAsync(ApiUrl, request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Brevo batch API error {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Brevo API returned {response.StatusCode}: {responseBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<BrevoBatchResponse>(cancellationToken);
        var messageIds = result?.MessageIds ?? [];

        var joinedIds = string.Join(", ", messageIds);
        logger.LogInformation("Batch email sent via Brevo: {Count} messages, messageIds: {MessageIds}",
            messages.Count, joinedIds);

        return messageIds;
    }

    private void EnsureHeaders(EmailSettings emailSettings)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("api-key", emailSettings.Brevo.ApiKey);
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
    }
}

#region Brevo API models

internal class BrevoSendRequest
{
    [JsonPropertyName("sender")]
    public required BrevoContact Sender { get; set; }

    [JsonPropertyName("to")]
    public required List<BrevoContact> To { get; set; }

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("htmlContent")]
    public required string HtmlContent { get; set; }
}

internal class BrevoBatchRequest
{
    [JsonPropertyName("sender")]
    public required BrevoContact Sender { get; set; }

    [JsonPropertyName("messageVersions")]
    public required List<BrevoMessageVersion> MessageVersions { get; set; }
}

internal class BrevoMessageVersion
{
    [JsonPropertyName("to")]
    public required List<BrevoContact> To { get; set; }

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("htmlContent")]
    public required string HtmlContent { get; set; }
}

internal class BrevoContact
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class BrevoSendResponse
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}

internal class BrevoBatchResponse
{
    [JsonPropertyName("messageIds")]
    public List<string>? MessageIds { get; set; }
}

#endregion
