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

    public class StressPartitionSender : StressSenderBase
    {
        private readonly IList<PartitionSender> partitionSenders = new List<PartitionSender>();
        private readonly IStressMetrics stressMetrics;
        private readonly ILogger<StressPartitionSender> logger;
        private List<Task> sendTasks = new List<Task>(4);

        public StressPartitionSender(IConfiguration configuration, IStressMetrics stressMetrics, ILogger<StressPartitionSender> logger)
            : base(configuration, logger)
        {
            this.stressMetrics = stressMetrics;
            this.logger = logger;
        }


        public async override Task EnsureInitializedAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var information = await GetRuntimeInformationAsync(cancellationToken);

            var sortedPartitionIds = new string[information.PartitionIds.Length];
            Array.Copy(information.PartitionIds, 0, sortedPartitionIds, 0, sortedPartitionIds.Length);
            Array.Sort(sortedPartitionIds);

            foreach (var partitionId in sortedPartitionIds)
            {
                partitionSenders.Add(eventHubClient.CreatePartitionSender(partitionId));
            }

            this.logger.LogInformation($"Initialized {nameof(StressPartitionSender)} sender");

            await base.EnsureInitializedAsync(batchSize, cancellationToken);
        }

        public override async Task SendAsync(EventData[] messages, CancellationToken cancellationToken = default)
        {
            var messagePerPartition = messages.Length / partitionSenders.Count;

            sendTasks.Clear();

            for (var i = 0; i < this.partitionSenders.Count; i++)
            {
                var batch = new EventData[messagePerPartition];
                // we explicitly want a new set of messages not a segment
                Array.Copy(messages, i * messagePerPartition, batch, 0, messagePerPartition);

                sendTasks.Add(StartSendAsync(i, batch));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task StartSendAsync(int partitionIdx, EventData[] messages)
        {
            var sw = stopWatches[partitionIdx];
            sw.Restart();

            var sender = partitionSenders[partitionIdx];

            await sender.SendAsync(messages);
            sw.Stop();
            var elapsed = sw.Elapsed.TotalMilliseconds;
            var partitionId = partitionIdx.ToString();

            stressMetrics.TrackPublishDuration(elapsed, partitionId);
            stressMetrics.TrackNumberOfMessages(messages.Length, partitionId);
            stressMetrics.TrackBatchSize(messages.Sum(x => x.Body.Count), partitionId);
        }

        protected override ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                Task.WhenAll(partitionSenders.Select(x => x.CloseAsync()));
            }
            return base.DisposeAsync(disposing);
        }
    }
}
