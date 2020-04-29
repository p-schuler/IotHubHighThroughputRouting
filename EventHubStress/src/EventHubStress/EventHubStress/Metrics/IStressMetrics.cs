namespace EventHubStress.Metrics
{
    public interface IStressMetrics
    {
        void TrackPublishDuration(double value, string partitionId = "N/A");
        void TrackNumberOfMessages(double value, string partitionId = "N/A");
        void TrackBatchSize(double value, string partitionId = "N/A");
    }
}
