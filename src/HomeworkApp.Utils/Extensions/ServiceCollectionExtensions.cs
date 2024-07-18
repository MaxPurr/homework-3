using Microsoft.Extensions.DependencyInjection;

namespace HomeworkApp.Utils.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddUtils(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}