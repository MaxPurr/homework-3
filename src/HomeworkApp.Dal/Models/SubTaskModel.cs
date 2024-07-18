namespace HomeworkApp.Dal.Models;

public record SubTaskModel
{
    public required long TaskId { get; init; }
    public required string Title { get; init; }
    public required HomeworkApp.Dal.Enums.TaskStatus Status { get; init; }
    public required long[] ParentTaskIds { get; init; }
}