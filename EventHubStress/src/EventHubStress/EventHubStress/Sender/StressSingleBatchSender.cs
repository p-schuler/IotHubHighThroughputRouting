namespace EventHubStress.Sender
{
    using EventHubStress.Metrics;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class StressSingleBatchSender : StressSenderBase
    {
        private readonly IStressMetrics stressMetrics;
        private readonly ILogger<StressPartitionSender> logger;
        private List<Task> sendTasks = new List<Task>(4);
        private Random rnd = new Random();
        private int targetPartitionCount;

        public StressSingleBatchSender(IConfiguration configuration, IStressMetrics stressMetrics, ILogger<StressPartitionSender> logger)
            : base(configuration, logger)
        {
            this.stressMetrics = stressMetrics;
            this.logger = logger;
        }


        public async override Task EnsureInitializedAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation($"Initialized {nameof(StressSingleBatchSender)} sender");
            InitializeStopWatches(batchSize);

            targetPartitionCount = await GetNumberOfPartitionsAsync(cancellationToken);
        }

        public override async Task SendAsync(EventData[] messages, CancellationToken cancellationToken = default)
        {
            sendTasks.Clear();

            for (var i = 0; i < messages.Length; i++)
            {
                sendTasks.Add(StartSend(i, messages[i]));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task StartSend(int idx, EventData message)
        {
            // when connected to an iot hub, we use the source partition as the key
            // to ensure the messages from the same device arrive on the same partition 
            // in the event hub. This is just simulating that behavior.
            var key = (rnd.Next(0, 32)).ToString();

            var batch = eventHubClient.CreateBatch(new BatchOptions { PartitionKey = key });
            batch.TryAdd(message);
            
            var sw = stopWatches[idx];
            sw.Restart();

            await eventHubClient.SendAsync(batch);

            sw.Stop();
            var elapsed = sw.Elapsed.TotalMilliseconds;
            stressMetrics.TrackPublishDuration(elapsed);
            stressMetrics.TrackNumberOfMessages(1);
            stressMetrics.TrackBatchSize(message.Body.Count);
        }
    }
}
