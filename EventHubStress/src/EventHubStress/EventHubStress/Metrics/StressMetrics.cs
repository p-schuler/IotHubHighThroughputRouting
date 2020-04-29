namespace EventHubStress.Metrics
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.Extensions.Logging;
    using System;

    public class StressMetrics : IStressMetrics
    {
        private const string MetricNamespace = "ehstress";
        private const string MetricIdPublishingPerf = "Publish Performance";
        private const string MetricIdNumberOfMessages = "Number Of Messages";
        private const string MetricIdPerformanceBucket = "Performance Buckets";
        private const string MetricIdBatchSize = "Batch Size";
        private const string PerformanceBucket = "Perf Bucket";
        private const string TargetPartitionId = "Partition Id";
        private const string MachineName = "Machine Name";

        private readonly Metric publishMetric;
        private readonly Metric perfBucketMetric;
        private readonly Metric numberOfMessagesMetric;
        private readonly Metric batchSizeMetric;
        private readonly ILogger<StressMetrics> logger;

        public StressMetrics(TelemetryClient telemetryClient, ILogger<StressMetrics> logger)
        {
            this.publishMetric = telemetryClient.GetMetric(new MetricIdentifier(MetricNamespace, MetricIdPublishingPerf, PerformanceBucket, TargetPartitionId, MachineName));
            this.perfBucketMetric = telemetryClient.GetMetric(new MetricIdentifier(MetricNamespace, MetricIdPerformanceBucket, PerformanceBucket, TargetPartitionId, MachineName));
            this.numberOfMessagesMetric = telemetryClient.GetMetric(new MetricIdentifier(MetricNamespace, MetricIdNumberOfMessages, TargetPartitionId, MachineName));
            this.batchSizeMetric = telemetryClient.GetMetric(new MetricIdentifier(MetricNamespace, MetricIdBatchSize, TargetPartitionId, MachineName));
            this.logger = logger;
        }

        public void TrackBatchSize(double value, string partitionId = "N/A")
        {
            batchSizeMetric.TrackValue(value, partitionId, Environment.MachineName);
        }

        public void TrackNumberOfMessages(double value, string partitionId = "N/A")
        {
            numberOfMessagesMetric.TrackValue(value, partitionId, Environment.MachineName);
        }

        public void TrackPublishDuration(double value, string partitionId = "N/A")
        {
            var bucketLabel = GetPerfBucketName(value);

            publishMetric.TrackValue(value, bucketLabel, partitionId, Environment.MachineName);
            perfBucketMetric.TrackValue(1, bucketLabel, partitionId, Environment.MachineName);

            if (value > 1_000)
            {
                this.logger.LogWarning($"[{DateTime.UtcNow}] slow send on partition {partitionId}: {value}");
            }
        }

        private static string GetPerfBucketName(double elapsed)
        {
            if (elapsed < 500) return "< 500";
            if (elapsed < 1000) return "500 - 999";
            if (elapsed < 3000) return "1000 - 2999";
            if (elapsed < 5000) return "3000 - 4999";
            return "> 5000";
        }
    }
}
