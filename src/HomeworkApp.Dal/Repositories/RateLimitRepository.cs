using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HomeworkApp.Dal.Repositories;

public class RateLimitRepository : RedisRepository, IRateLimitRepository
{
    public RateLimitRepository(IOptions<DalOptions> dalSettings) : base(dalSettings.Value) { }

    protected override string KeyPrefix => "rate-limit";

    protected override TimeSpan KeyTtl => TimeSpan.FromMinutes(1);

    public async Task<bool> SetRequestsPerMinuteIfNotExists(string userIP, long requestsCount, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var connection = await GetConnection();
        var key = GetKey(userIP);
        bool wasSet = await connection.StringSetAsync(
            key, 
            requestsCount, 
            KeyTtl,
            When.NotExists);
        return wasSet;
    }

    public async Task<long> DecrementRemainingRequests(string userIP, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var connection = await GetConnection();
        var key = GetKey(userIP);
        long remainingRequests = await connection.StringDecrementAsync(key);
        return remainingRequests;
    }
}