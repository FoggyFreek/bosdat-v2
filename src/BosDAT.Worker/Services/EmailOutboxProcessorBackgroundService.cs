using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Data;
using BosDAT.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BosDAT.Worker.Services;

public class EmailOutboxProcessorBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WorkerSettings> settings,
    ILogger<EmailOutboxProcessorBackgroundService> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EmailOutboxJob.Enabled)
        {
            logger.LogInformation("Email outbox processor is disabled");
            return;
        }

        var pollingInterval = TimeSpan.FromSeconds(_settings.EmailOutboxJob.PollingIntervalSeconds);
        logger.LogInformation(
            "Email outbox processor started. Polling every {Interval}s, batch size {BatchSize}",
            _settings.EmailOutboxJob.PollingIntervalSeconds,
            _settings.EmailOutboxJob.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
                await Task.Delay(pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in email outbox processor");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        logger.LogInformation("Email outbox processor stopped");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var templateRenderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();

        var pendingEmails = await dbContext.EmailOutboxMessages
            .Where(e => e.Status == EmailStatus.Pending
                && (e.NextAttemptAtUtc == null || e.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(_settings.EmailOutboxJob.BatchSize)
            .ToListAsync(ct);

        if (pendingEmails.Count == 0)
            return;

        logger.LogInformation("Processing {Count} pending email(s)", pendingEmails.Count);

        foreach (var email in pendingEmails)
        {
            await ProcessSingleEmailAsync(email, dbContext, emailSender, templateRenderer, ct);
        }
    }

    private async Task ProcessSingleEmailAsync(
        EmailOutboxMessage email,
        ApplicationDbContext dbContext,
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        CancellationToken ct)
    {
        try
        {
            email.MarkProcessing();
            await dbContext.SaveChangesAsync(ct);

            var htmlBody = await templateRenderer.RenderAsync(email.TemplateName,
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(email.TemplateDataJson)!, ct);
            var providerMessageId = await emailSender.SendAsync(email.To, email.Subject, htmlBody, ct);

            email.MarkSent(providerMessageId);
            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation(
                "Email {EmailId} sent to {To}, provider messageId: {MessageId}",
                email.Id, email.To, providerMessageId);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Email {EmailId} was already claimed by another processor", email.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email {EmailId} to {To}", email.Id, email.To);

            try
            {
                email.MarkFailed(ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message);
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Failed to update email {EmailId} failure status", email.Id);
            }
        }
    }
}
