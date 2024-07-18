namespace HomeworkApp.Dal.Repositories.Interfaces;

public interface IRateLimitRepository : IRedisRepository
{
    Task<bool> SetRequestsPerMinuteIfNotExists(string userIP, long requestsCount, CancellationToken token);
    
    Task<long> DecrementRemainingRequests(string userIP, CancellationToken token);
}