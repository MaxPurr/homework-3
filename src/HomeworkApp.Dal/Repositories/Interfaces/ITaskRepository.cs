using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Models;

namespace HomeworkApp.Dal.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<long[]> Add(TaskEntityV1[] tasks, CancellationToken token);
    
    Task<TaskEntityV1[]> Get(TaskGetModel query, CancellationToken token);

    Task Assign(AssignTaskModel model, CancellationToken token);
    
    Task<bool>  IsTaskExist(long taskId, CancellationToken token);

    Task SetParentTask(long taskId, long parentTaskId, CancellationToken token);
    
    Task<SubTaskModel[]> GetSubTasksInStatus(long parentTaskId, HomeworkApp.Dal.Enums.TaskStatus[] statuses, CancellationToken token);
}