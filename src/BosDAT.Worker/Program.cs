using System.Net;
using BosDAT.Worker.Configuration;
using BosDAT.Worker.Services;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<WorkerSettings>(
    builder.Configuration.GetSection(WorkerSettings.SectionName));

var workerSettings = builder.Configuration
    .GetSection(WorkerSettings.SectionName)
    .Get<WorkerSettings>();

if (workerSettings == null)
{
    throw new InvalidOperationException("WorkerSettings configuration is missing");
}

builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient("BosApiAuth", client =>
{
    client.BaseAddress = new Uri(workerSettings.Api.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(workerSettings.Api.TimeoutSeconds);
});

builder.Services.AddHttpClient<IBosApiClient, BosApiClient>(client =>
{
    client.BaseAddress = new Uri(workerSettings.Api.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(workerSettings.Api.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("User-Agent", "BosDAT.Worker/1.0");
    client.DefaultRequestHeaders.Add("X-Worker-Identity", "BosDAT.Worker");
})
.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
.AddResilienceHandler("retry", resilienceBuilder =>
{
    resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = workerSettings.Api.RetryCount,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests
                             || (int)r.StatusCode >= 500)
    });
});

builder.Services.AddHostedService<InvoiceRunBackgroundService>();
builder.Services.AddHostedService<LessonGenerationBackgroundService>();
builder.Services.AddHostedService<LessonStatusUpdateBackgroundService>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("BosDAT Worker starting...");
logger.LogInformation("API Base URL: {BaseUrl}", workerSettings.Api.BaseUrl);
logger.LogInformation("Invoice Job Enabled: {Enabled}, Day of Month: {Day}",
    workerSettings.InvoiceJob.Enabled, workerSettings.InvoiceJob.DayOfMonth);
logger.LogInformation("Lesson Generation Job Enabled: {Enabled}, Days Ahead: {Days}",
    workerSettings.LessonGenerationJob.Enabled, workerSettings.LessonGenerationJob.DaysAhead);
logger.LogInformation("Lesson Status Update Job Enabled: {Enabled}",
    workerSettings.LessonStatusUpdateJob.Enabled);

await host.RunAsync();
