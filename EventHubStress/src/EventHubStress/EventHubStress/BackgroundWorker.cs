namespace EventHubStress
{
    using EventHubStress.Sender;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class BackgroundWorker : IHostedService, IAsyncDisposable
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        
        private readonly ILogger<BackgroundWorker> logger;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IStressSender stressSender;
        private readonly IConfiguration configuration;
        protected IList<byte[]> randomPayloads;

        private Task mainTask;
        private int numberOfMessagesPerBatchPerPartition = 5;
        private int delayBetweenBatchesInMs = 200;

        public BackgroundWorker(
                                ILogger<BackgroundWorker> logger, 
                                IHostApplicationLifetime applicationLifetime, 
                                IStressSender stressSender,
                                IConfiguration configuration)
        {
            this.logger = logger;
            this.applicationLifetime = applicationLifetime;
            this.stressSender = stressSender;
            this.configuration = configuration;
        }

        public ValueTask DisposeAsync()
        {
            this.logger.LogInformation("Disposed.");
            return new ValueTask();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"GC Server Mode: {System.Runtime.GCSettings.IsServerGC}, Latency Mode: {System.Runtime.GCSettings.LatencyMode}");

            mainTask = Task.Run(async () => 
            {
                Initialize();

                logger.LogInformation("Start sending messages.");
                var numberOfPartitions = await this.stressSender.GetNumberOfPartitionsAsync(cts.Token);
                var batchSize = numberOfPartitions * numberOfMessagesPerBatchPerPartition;

                logger.LogInformation($"Configuration: Sending {batchSize} messages distributed to {numberOfMessagesPerBatchPerPartition} per partition. Delay between batches: {delayBetweenBatchesInMs}ms");

                await stressSender.EnsureInitializedAsync(batchSize, cts.Token);

                while (!cts.IsCancellationRequested && !this.applicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    var messages = PrepareBatch(batchSize);
                    await stressSender.SendAsync(messages, cts.Token);

                    try
                    {
                        await Task.Delay(delayBetweenBatchesInMs, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                logger.LogInformation("Stop sending messages.");
            });

            return mainTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cts.Cancel();
            await Task.WhenAny(this.mainTask, Task.Delay(-1, cancellationToken));
        }

        private EventData[] PrepareBatch(int batchSize)
        {
            var messages = new EventData[batchSize];
            for (var i = 0; i < batchSize; i++)
            {
                messages[i] = new EventData(randomPayloads[i % randomPayloads.Count]);
            }

            return messages;
        }

        private void Initialize()
        {
            const int numberOfPayloads = 500;
            randomPayloads = new List<byte[]>(numberOfPayloads);
            var rnd = new Random();

            for (var i = 0; i < numberOfPayloads; i++)
            {
                var buffer = new byte[512];
                rnd.NextBytes(buffer);
                randomPayloads.Add(buffer);
            }

            ConfigureMessageSending();
        }

        private void ConfigureMessageSending()
        {
            var messages = configuration.GetValue<int>("NumberOfMessagesPerBatchPerPartition");
            if (messages > 0)
            {
                numberOfMessagesPerBatchPerPartition = messages;
            }

            var delay = configuration.GetValue<int>("DelayBetweenBatchesInMs");
            if (delay > 0)
            {
                delayBetweenBatchesInMs = delay;
            }
        }
    }
}
