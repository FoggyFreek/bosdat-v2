using BosDAT.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace BosDAT.Worker.Services;

public class InvoiceRunBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WorkerSettings> settings,
    ILogger<InvoiceRunBackgroundService> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = settings.Value;
    private bool _hasRunToday;
    private DateOnly _lastRunDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.InvoiceJob.Enabled)
        {
            logger.LogInformation("Invoice run background service is disabled");
            return;
        }

        logger.LogInformation(
            "Invoice run background service started. Configured to run on day {DayOfMonth} at {Time}",
            _settings.InvoiceJob.DayOfMonth,
            _settings.InvoiceJob.ExecutionTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var today = DateOnly.FromDateTime(now);
                var currentTime = TimeOnly.FromDateTime(now);

                if (today != _lastRunDate)
                {
                    _hasRunToday = false;
                    _lastRunDate = today;
                }

                if (ShouldRunJob(today, currentTime))
                {
                    await RunInvoiceJobAsync(stoppingToken);
                    _hasRunToday = true;
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in invoice run background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        logger.LogInformation("Invoice run background service stopped");
    }

    private bool ShouldRunJob(DateOnly today, TimeOnly currentTime)
    {
        if (_hasRunToday)
            return false;

        if (today.Day != _settings.InvoiceJob.DayOfMonth)
            return false;

        return currentTime >= _settings.InvoiceJob.ExecutionTime;
    }

    private async Task RunInvoiceJobAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var previousMonth = now.AddMonths(-1);

        logger.LogInformation(
            "Starting invoice run for {Month}/{Year}",
            previousMonth.Month,
            previousMonth.Year);

        using var scope = serviceProvider.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IBosApiClient>();

        var result = await apiClient.TriggerInvoiceRunAsync(
            previousMonth.Month,
            previousMonth.Year,
            stoppingToken);

        if (result != null)
        {
            logger.LogInformation(
                "Invoice run completed successfully: {InvoicesGenerated} invoices generated for {Month}/{Year}, total amount: {TotalAmount:C}",
                result.InvoicesGenerated,
                result.Month,
                result.Year,
                result.TotalAmount);
        }
        else
        {
            logger.LogWarning("Invoice run returned no result for {Month}/{Year}", previousMonth.Month, previousMonth.Year);
        }
    }
}
