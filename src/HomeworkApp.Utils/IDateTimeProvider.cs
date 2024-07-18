namespace HomeworkApp.Utils;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}