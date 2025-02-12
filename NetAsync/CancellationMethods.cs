using System.Net.NetworkInformation;

namespace NetAsync;

public class CancellationMethods
{
    public async Task<HttpResponseMessage> GetWithTimeoutAsync(HttpClient client, string url, CancellationToken cancellationToken)
    { 
        //will be cancelled if cancellationToken was cancelled or cts was cancelled
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); 
        cts.CancelAfter(TimeSpan.FromSeconds(2)); 
        CancellationToken combinedToken = cts.Token; 
        return 
            await client.GetAsync(url, combinedToken);
    }

    async Task<PingReply> PingAsync(string hostNameOrAddress, CancellationToken cancellationToken)
    {
        using var ping = new Ping();
        Task<PingReply> task = ping.SendPingAsync(hostNameOrAddress);
        using CancellationTokenRegistration _ = cancellationToken.Register(() => ping.SendAsyncCancel());
        return await task;
    }
}