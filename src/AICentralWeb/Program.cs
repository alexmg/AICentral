using AICentral;
using AICentral.AzureAISearchVectorizer;
using AICentral.Configuration;
using AICentral.Dapr.Broadcast;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using AICentral.Logging.PIIStripping;
using AICentral.QuickStarts;
using AICentral.RateLimiting.DistributedRedis;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddSimpleConsole(options =>
{
    options.SingleLine = true;
});

if (builder.Environment.EnvironmentName != "tests" && Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") != null)
{
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics => { metrics.AddMeter(ActivitySource.AICentralTelemetryName); })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddSource(ActivitySource.AICentralTelemetryName);
        })
        .UseAzureMonitor(options => options.SamplingRatio = 0.1f);
}

if (builder.Environment.EnvironmentName != "tests" &&
    Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") != null)
{
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics => { metrics.AddMeter(ActivitySource.AICentralTelemetryName); })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddSource(ActivitySource.AICentralTelemetryName);
        })
        .UseOtlpExporter();
}

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSimpleConsole(options =>
{
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.SingleLine = true;
}));
var startupLogger = loggerFactory.CreateLogger("AICentral.Startup");

builder.Services.AddCors();
builder.Services.AddHealthChecks();

if (builder.Environment.EnvironmentName == "APImProxyWithCosmosLogging")
{
    var config = new APImProxyWithCosmosLogging.Config();
    builder.Configuration.Bind("AICentral", config);
    var assembler = APImProxyWithCosmosLogging.BuildAssembler(config);

    assembler.AddServices(
        builder.Services,
        startupLogger: startupLogger,
        optionalHandler: null);
}
else if (builder.Environment.EnvironmentName == "DaprAudit")
{
    var config = new DaprAudit.Config();
    builder.Configuration.Bind("AICentral", config);
    var assembler = DaprAudit.BuildAssembler(config);

    assembler.AddServices(
        builder.Services,
        startupLogger: startupLogger,
        optionalHandler: null);
}
else
{
    builder.Services.AddAICentral(
        builder.Configuration,
        startupLogger: startupLogger,
        additionalComponentAssemblies:
        [
            typeof(AzureMonitorLoggerFactory).Assembly,
            typeof(PIIStrippingLogger).Assembly,
            typeof(DistributedRateLimiter).Assembly,
            typeof(AdaptJsonToAzureAISearchTransformer).Assembly,
            typeof(DaprBroadcaster).Assembly
        ]);
}

var enableSummaryPage = builder.Configuration.GetValue<bool>("EnableAICentralSummaryWebPage");

if (enableSummaryPage)
{
    builder.Services.AddRazorPages();
}

var app = builder.Build();

if (enableSummaryPage)
{
    app.MapRazorPages();
}

app.MapHealthChecks("/healthz");

app.UseCors(corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAICentral();

app.Run();

namespace AICentralWeb
{
    public partial class Program
    {
    }
}