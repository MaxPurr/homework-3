using Grpc.Core;
using Grpc.Core.Interceptors;
using HomeworkApp.Bll.Services.Interfaces;

namespace HomeworkApp.Interceptors;

public class RateLimitInterceptor : Interceptor
{
    private const string userIPKey = "X-R256-USER-IP";
    private readonly IRateLimiterService _rateLimiterService;

    public RateLimitInterceptor(IRateLimiterService rateLimiterService)
    {
        _rateLimiterService = rateLimiterService;
    }
    
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var userIP = context.RequestHeaders.GetValue(userIPKey);
        bool isRateLimitExceeded = await _rateLimiterService.IsRateLimitExceeded(userIP, context.CancellationToken);
        if (isRateLimitExceeded)
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted, "Limit of requests per minute has been exceeded."));
        }
        return await continuation(request, context);
    }
}