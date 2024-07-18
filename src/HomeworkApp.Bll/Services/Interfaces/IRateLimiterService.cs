namespace HomeworkApp.Bll.Services.Interfaces;

public interface IRateLimiterService
{
    Task<bool> IsRateLimitExceeded(string userIP, CancellationToken token);
}