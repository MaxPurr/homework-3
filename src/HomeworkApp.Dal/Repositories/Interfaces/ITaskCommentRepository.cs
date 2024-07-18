using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Entities;

namespace HomeworkApp.Dal.Repositories.Interfaces;

public interface ITaskCommentRepository
{
    Task<long> Add(TaskCommentEntityV1 model, CancellationToken token);
    Task Update(TaskCommentEntityV1 model, CancellationToken token);
    Task SetDeleted(long taskCommentId, CancellationToken token);
    Task<TaskCommentEntityV1[]> Get(TaskCommentGetModel model, CancellationToken token);
    Task<bool> IsTaskCommentExist(long taskCommentId, CancellationToken token);
}