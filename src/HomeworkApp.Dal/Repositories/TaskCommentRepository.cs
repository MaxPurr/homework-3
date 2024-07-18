using System.Text;
using Dapper;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Exceptions;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Settings;
using HomeworkApp.Utils;
using Microsoft.Extensions.Options;

namespace HomeworkApp.Dal.Repositories;

public class TaskCommentRepository : PgRepository, ITaskCommentRepository
{
    private IDateTimeProvider _dateTimeProvider;
    public TaskCommentRepository(IOptions<DalOptions> dalSettings, IDateTimeProvider dateTimeProvider) : base(dalSettings.Value)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<long> Add(TaskCommentEntityV1 model, CancellationToken token)
    {
        const string sqlQuery = @"
               insert into task_comments (task_id, author_user_id, message, at)  
               select task_id, author_user_id, message, at
                 from UNNEST(@TaskComments)
            returning id;
        ";

        await using var connection = await GetConnection();
        long id = await connection.QuerySingleAsync<long>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskComments = new[] { model }
                },
                cancellationToken: token));
        return id;
    }

    public async Task Update(TaskCommentEntityV1 model, CancellationToken token)
    {
        bool isTaskCommentExist = await IsTaskCommentExist(model.Id, token);
        if (!isTaskCommentExist)
        {
            throw new RecordNotFoundException(model.Id);
        }

        const string sqlQuery = @"
            update task_comments tc
               set message = @Message
                 , modified_at = @ModifiedAt
             where tc.id = @TaskCommentId;
        ";
        
        DateTimeOffset modifiedAt = _dateTimeProvider.UtcNow;
        
        await using var connection = await GetConnection();
        await connection.QueryAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskCommentId = model.Id,
                    Message = model.Message,
                    ModifiedAt = modifiedAt
                },
                cancellationToken: token));
    }

    public async Task SetDeleted(long taskCommentId, CancellationToken token)
    {
        bool isTaskCommentExist = await IsTaskCommentExist(taskCommentId, token);
        if (!isTaskCommentExist)
        {
            throw new RecordNotFoundException(taskCommentId);
        }
        
        const string sqlQuery = @"
           update task_comments
              set deleted_at = @DeletedAt
            where id = @TaskCommentId;
        ";

        DateTimeOffset deletedAt = _dateTimeProvider.UtcNow;
        await using var connection = await GetConnection();
        await connection.QueryAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    DeletedAt = deletedAt,
                    TaskCommentId = taskCommentId
                },
                cancellationToken: token));
    }

    public async Task<TaskCommentEntityV1[]> Get(TaskCommentGetModel model, CancellationToken token)
    {
        StringBuilder sqlQueryBuilder = new StringBuilder();
        sqlQueryBuilder.Append(@"
            select id
                 , task_id
                 , author_user_id
                 , message
                 , at
                 , modified_at
                 , deleted_at
              from task_comments
             where task_id = @TaskId
        ");

        if (!model.IncludeDeleted)
        {
            sqlQueryBuilder.Append(" and deleted_at is null");
        }

        sqlQueryBuilder.Append(" order by at desc;");
        
        await using var connection = await GetConnection();
        var taskComments = await connection.QueryAsync<TaskCommentEntityV1>(
            new CommandDefinition(
                sqlQueryBuilder.ToString(),
                new
                {
                    TaskId = model.TaskId,
                },
                cancellationToken: token));
        return taskComments.ToArray();
    }

    public async Task<bool> IsTaskCommentExist(long taskCommentId, CancellationToken token)
    {
        const string sqlQuery = @"
            select exists(select 1 
                            from task_comments tc 
                           where tc.id = @TaskCommentId);
        ";
        
        await using var connection = await GetConnection();
        bool isExist = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskCommentId = taskCommentId
                },
                cancellationToken: token));
        return isExist;
    }
}