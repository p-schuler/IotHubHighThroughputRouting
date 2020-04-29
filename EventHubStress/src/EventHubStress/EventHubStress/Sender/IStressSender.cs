namespace EventHubStress.Sender
{
    using Microsoft.Azure.EventHubs;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IStressSender
    {
        Task SendAsync(EventData[] messages, CancellationToken cancellationToken = default);
        Task EnsureInitializedAsync(int batchSize, CancellationToken cancellationToken = default);
        Task<int> GetNumberOfPartitionsAsync(CancellationToken token = default);
    }
}
