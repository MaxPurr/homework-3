using AutoBogus;
using Bogus;
using HomeworkApp.Dal.Entities;
using HomeworkApp.IntegrationTests.Creators;

namespace HomeworkApp.IntegrationTests.Fakers;

public static class TaskCommentEntityV1Faker
{
    private static readonly object Lock = new();

    private static readonly Faker<TaskCommentEntityV1> Faker = new AutoFaker<TaskCommentEntityV1>()
        .RuleFor(x => x.Id, _ => Create.RandomId())
        .RuleFor(x => x.AuthorUserId, f => f.Random.Long(0L))
        .RuleFor(x => x.Message, f => f.Random.String2(15, 30))
        .RuleFor(x => x.At, f => Create.RandomUtcTime())
        .RuleFor(x => x.ModifiedAt, _ => null)
        .RuleFor(x => x.DeletedAt, _ => null);
    
    public static TaskCommentEntityV1 Generate(long taskId)
    {
        lock (Lock)
        {
            return Faker.Generate() with { TaskId = taskId };
        }
    }

    public static TaskCommentEntityV1 WithId(
        this TaskCommentEntityV1 taskCommentEntityV1,
        long id)
    {
        return taskCommentEntityV1 with { Id = id };
    }
}