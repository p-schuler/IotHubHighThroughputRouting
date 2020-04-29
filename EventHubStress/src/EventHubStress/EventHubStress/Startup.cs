namespace EventHubStress
{
    using EventHubStress.Metrics;
    using EventHubStress.Sender;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Configuration;
    using System.Runtime;

    internal class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            SetupAppInsights(services);

            services.AddSingleton<IStressMetrics, StressMetrics>();
            services.AddSingleton<IStressSender, StressPartitionSender>();
            //services.AddSingleton<IStressSender, StressSingleBatchSender>();

            services.AddHostedService<BackgroundWorker>();
        }

        private void SetupAppInsights(IServiceCollection services)
        {
            const string appInsightsKeyName = "ApplicationInsights:InstrumentationKey";
            var appInsightsKey = this.configuration.GetValue<string>(appInsightsKeyName);
            if (string.IsNullOrEmpty(appInsightsKey))
            {
                throw new ConfigurationErrorsException($"Missing {appInsightsKeyName}");
            }

            services.AddApplicationInsightsTelemetry(appInsightsKey);
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
        {
            var disableSustainedLatency = this.configuration.GetValue<bool>("GCDisableSustainedLatency");
            if (!disableSustainedLatency)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
            logger.LogInformation($"GC Server Mode: {GCSettings.IsServerGC}, Latency Mode: {GCSettings.LatencyMode}");
        }
    }
}