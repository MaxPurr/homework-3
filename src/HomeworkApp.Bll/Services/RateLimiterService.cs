using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Repositories.Interfaces;

namespace HomeworkApp.Bll.Services;

public class RateLimiterService : IRateLimiterService
{
    private const long requestsPerMinute = 100L;
    private readonly IRateLimitRepository _rateLimitRepository;

    public RateLimiterService(IRateLimitRepository rateLimitRepository)
    {
        _rateLimitRepository = rateLimitRepository;
    }

    public async Task<bool> IsRateLimitExceeded(string userIP, CancellationToken token)
    {
        bool wasSet = await _rateLimitRepository
            .SetRequestsPerMinuteIfNotExists(userIP, requestsPerMinute, token);
        if (wasSet)
        {
            return false;
        }
        long remainingRequests = await _rateLimitRepository.DecrementRemainingRequests(userIP, token);
        if (remainingRequests > 0)
        {
            return false;
        }
        return true;
    }
}