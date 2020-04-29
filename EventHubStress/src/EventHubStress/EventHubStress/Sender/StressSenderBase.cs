namespace EventHubStress.Sender
{
    using Microsoft.Azure.EventHubs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class StressSenderBase : IStressSender, IAsyncDisposable
    {
        protected readonly EventHubClient eventHubClient;
        private readonly ILogger logger;
        protected IList<Stopwatch> stopWatches;

        private EventHubRuntimeInformation runtimeInformation;

        public StressSenderBase(IConfiguration configuration, ILogger logger)
        {
            const string connectionStringKeyName = "EventHub:ConnectionString";
            var connectionString = configuration.GetValue<string>(connectionStringKeyName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ConfigurationErrorsException($"Missing {connectionStringKeyName}");
            }

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString);
            this.logger = logger;
        }

        public virtual async Task EnsureInitializedAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var information = await GetRuntimeInformationAsync(cancellationToken);
            InitializeStopWatches(information.PartitionCount);
            this.logger.LogInformation($"Configured sender with {information.PartitionCount} partitions.");
        }

        protected void InitializeStopWatches(int numberOfStopWatches)
        {
            stopWatches = new List<Stopwatch>(numberOfStopWatches);

            for (var i = 0; i < numberOfStopWatches; i++)
            {
                this.stopWatches.Add(new Stopwatch());
            }
        }

        public abstract Task SendAsync(EventData[] messages, CancellationToken cancellationToken = default);

        public async Task<int> GetNumberOfPartitionsAsync(CancellationToken token = default)
        {
            return (await GetRuntimeInformationAsync()).PartitionCount;
        }

        protected async Task<EventHubRuntimeInformation> GetRuntimeInformationAsync(CancellationToken cancellationToken = default)
        {
            return runtimeInformation ?? (runtimeInformation = await eventHubClient.GetRuntimeInformationAsync());
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsync(true);
        }

        protected async virtual ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await eventHubClient.CloseAsync();
            }
        }
    }
}
