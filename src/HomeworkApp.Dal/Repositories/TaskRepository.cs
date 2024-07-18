using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Exceptions;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Microsoft.Extensions.Options;

namespace HomeworkApp.Dal.Repositories;

public class TaskRepository : PgRepository, ITaskRepository
{
    public TaskRepository(
        IOptions<DalOptions> dalSettings) : base(dalSettings.Value)
    {
    }

    public async Task<long[]> Add(TaskEntityV1[] tasks, CancellationToken token)
    {
        const string sqlQuery = @"
insert into tasks (parent_task_id, number, title, description, status, created_at, created_by_user_id, assigned_to_user_id, completed_at) 
select parent_task_id, number, title, description, status, created_at, created_by_user_id, assigned_to_user_id, completed_at
  from UNNEST(@Tasks)
returning id;
";

        await using var connection = await GetConnection();
        var ids = await connection.QueryAsync<long>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    Tasks = tasks
                },
                cancellationToken: token));
        
        return ids
            .ToArray();
    }

    public async Task<TaskEntityV1[]> Get(TaskGetModel query, CancellationToken token)
    {
        var baseSql = @"
select id
     , parent_task_id
     , number
     , title
     , description
     , status
     , created_at
     , created_by_user_id
     , assigned_to_user_id
     , completed_at
  from tasks
";
        
        var conditions = new List<string>();
        var @params = new DynamicParameters();

        if (query.TaskIds.Any())
        {
            conditions.Add($"id = ANY(@TaskIds)");
            @params.Add($"TaskIds", query.TaskIds);
        }
        
        var cmd = new CommandDefinition(
            baseSql + $" WHERE {string.Join(" AND ", conditions)} ",
            @params,
            commandTimeout: DefaultTimeoutInSeconds,
            cancellationToken: token);
        
        await using var connection = await GetConnection();
        return (await connection.QueryAsync<TaskEntityV1>(cmd))
            .ToArray();
    }

    public async Task Assign(AssignTaskModel model, CancellationToken token)
    {
        const string sqlQuery = @"
update tasks
   set assigned_to_user_id = @AssignToUserId
     , status = @Status
 where id = @TaskId
";

        await using var connection = await GetConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskId = model.TaskId,
                    AssignToUserId = model.AssignToUserId,
                    Status = model.Status
                },
                cancellationToken: token));
    }

    public async Task<bool> IsTaskExist(long taskId, CancellationToken token)
    {
        const string sqlQuery = @"
            select exists(select 1 
                            from tasks t 
                           where t.id = @TaskId);
        ";
        
        await using var connection = await GetConnection();
        bool isExist = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskId = taskId
                },
                cancellationToken: token));
        return isExist;
    }

    public async Task SetParentTask(long taskId, long parentTaskId ,CancellationToken token)
    {
        if (taskId == parentTaskId)
        {
            throw new CyclicDependenceException(taskId);
        }
        bool taskExists = await IsTaskExist(taskId, token);
        if (!taskExists)
        {
            throw new RecordNotFoundException(taskId);
        }
        bool parentTaskExists = await IsTaskExist(parentTaskId, token);
        if (!parentTaskExists)
        {
            throw new RecordNotFoundException(parentTaskId);
        }
        
        const string sqlQuery = @"
           update tasks t
              set parent_task_id = @ParentTaskId
            where id = @TaskId;
        ";
        
        await using var connection = await GetConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskId = taskId,
                    ParentTaskId = parentTaskId
                },
                cancellationToken: token));
    }

    public async Task<SubTaskModel[]> GetSubTasksInStatus(long parentTaskId,
        HomeworkApp.Dal.Enums.TaskStatus[] statuses, CancellationToken token)
    {
        bool taskExists = await IsTaskExist(parentTaskId, token);
        if (!taskExists)
        {
            throw new RecordNotFoundException(parentTaskId);
        }
        
        const string sqlQuery = @"
          with recursive child_tasks
            as (select t.id
                     , t.title
                     , t.status
                     , array[t.parent_task_id] as parent_task_ids
                  from tasks t
                 where t.parent_task_id = @ParentTaskId
                 union all
                select t.id
                     , t.title
                     , t.status
                     , cht.parent_task_ids || t.parent_task_id
                  from tasks t
                  join child_tasks cht on t.parent_task_id = cht.id)
        select *
          from child_tasks cht
         where cht.status = ANY(@Statuses);
        ";
        
        await using var connection = await GetConnection();
        var subTasks = await connection.QueryAsync<SubTaskModel>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    ParentTaskId = parentTaskId,
                    Statuses = statuses.Select(s => (int)s).ToArray(),
                },
                cancellationToken: token));
        return subTasks.ToArray();
    }
}