using FluentAssertions;
using HomeworkApp.Dal.Exceptions;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.IntegrationTests.Creators;
using HomeworkApp.IntegrationTests.Fakers;
using HomeworkApp.IntegrationTests.Fixtures;
using Xunit;
using TaskStatus = HomeworkApp.Dal.Enums.TaskStatus;

namespace HomeworkApp.IntegrationTests.RepositoryTests;

[Collection(nameof(TestFixture))]
public class TaskRepositoryTests
{
    private readonly ITaskRepository _repository;

    public TaskRepositoryTests(TestFixture fixture)
    {
        _repository = fixture.TaskRepository;
    }

    [Fact]
    public async Task Add_Task_Success()
    {
        // Arrange
        const int count = 5;

        var tasks = TaskEntityV1Faker.Generate(count);
        
        // Act
        var results = await _repository.Add(tasks, default);

        // Asserts
        results.Should().HaveCount(count);
        results.Should().OnlyContain(x => x > 0);
    }
    
    [Fact]
    public async Task Get_SingleTask_Success()
    {
        // Arrange
        var tasks = TaskEntityV1Faker.Generate();
        var taskIds = await _repository.Add(tasks, default);
        var expectedTaskId = taskIds.First();
        var expectedTask = tasks.First()
            .WithId(expectedTaskId);
        
        // Act
        var results = await _repository.Get(new TaskGetModel()
        {
            TaskIds = new[] { expectedTaskId }
        }, default);
        
        // Asserts
        results.Should().HaveCount(1);
        var task = results.Single();

        task.Should().BeEquivalentTo(expectedTask);
    }
    
    [Fact]
    public async Task AssignTask_Success()
    {
        // Arrange
        var assigneeUserId = Create.RandomId();
        
        var tasks = TaskEntityV1Faker.Generate();
        var taskIds = await _repository.Add(tasks, default);
        var expectedTaskId = taskIds.First();
        var expectedTask = tasks.First()
            .WithId(expectedTaskId)
            .WithAssignedToUserId(assigneeUserId);
        var assign = AssignTaskModelFaker.Generate()
            .First()
            .WithTaskId(expectedTaskId)
            .WithAssignToUserId(assigneeUserId);
        
        // Act
        await _repository.Assign(assign, default);
        
        // Asserts
        var results = await _repository.Get(new TaskGetModel()
        {
            TaskIds = new[] { expectedTaskId }
        }, default);
        
        results.Should().HaveCount(1);
        var task = results.Single();
        
        expectedTask = expectedTask with {Status = assign.Status};
        task.Should().BeEquivalentTo(expectedTask);
    }
    
    [Fact]
    public async Task IsTaskExist_TaskDoesNotExist_ReturnsFalse()
    {
        // Arrange
        long taskId = 404;
        bool expectedResult = false;

        // Act 
        bool actualResult = await _repository.IsTaskExist(taskId, default);

        // Asserts
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task IsTaskExist_TaskExists_ReturnsTrue()
    {
        // Arrange
        var task = TaskEntityV1Faker.Generate();
        long taskId = (await _repository.Add(task, default)).First();
        bool expectedResult = true;

        // Act 
        bool actualResult = await _repository.IsTaskExist(taskId, default);

        // Asserts
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task SetParentTask_TaskNotFound_ThrowsRecordNotFoundException()
    {
        // Arrange
        var parentTask = TaskEntityV1Faker.Generate();
        long parentTaskId = (await _repository.Add(parentTask, default)).First();
        
        long childTaskId = 404;
        
        // Act
        var setParentTaskAction = async () => 
            await _repository.SetParentTask(childTaskId, parentTaskId, default);
        
        // Asserts
        await setParentTaskAction.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task SetParentTask_ParentTaskNotFound_ThrowsRecordNotFoundException()
    {
        // Arrange 
        var childTask = TaskEntityV1Faker.Generate();
        long childTaskId = (await _repository.Add(childTask, default)).First();

        long parentTaskId = 404;
        
        // Act
        var setParentTaskAction = async () => 
            await _repository.SetParentTask(childTaskId, parentTaskId, default);
       
        // Asserts
        await setParentTaskAction.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task SetParentTask_TaskIdEqualsParentTaskId_ThrowsCyclicDependenceException()
    {
        // Arrange
        var childTask = TaskEntityV1Faker.Generate();
        long childTaskId = (await _repository.Add(childTask, default)).First();
        long parentTaskId = childTaskId;
        
        // Act
        var setParentTaskAction = async () => 
            await _repository.SetParentTask(childTaskId, parentTaskId, default);
        
        // Asserts
        await setParentTaskAction.Should().ThrowAsync<CyclicDependenceException>();
    }

    [Fact]
    public async Task SetParentTask_BothTasksExist_Success()
    {
        // Arrange
        var parentTask = TaskEntityV1Faker.Generate();
        long parentTaskId = (await _repository.Add(parentTask, default)).First();
        
        var childTask = TaskEntityV1Faker.Generate();
        long childTaskId = (await _repository.Add(childTask, default)).First();
        
        // Act 
        await _repository.SetParentTask(childTaskId, parentTaskId, default);
        var updatedTask = (await _repository.Get(new TaskGetModel()
        {
            TaskIds = new[] { childTaskId }
        }, default)).First();
        
        // Assert 
        updatedTask.ParentTaskId.Should().Be(parentTaskId);
    }

    [Fact]
    public async Task GetSubTasksInStatus_ParentTaskNotFound_ThrowsRecordNotFoundException()
    {
        // Arrange
        int parentTaskId = 404;
        var statuses = new TaskStatus[] { TaskStatus.ToDo, TaskStatus.InProgress };
        
        // Act
        var getSubTasksAction = async () => await _repository.GetSubTasksInStatus(parentTaskId, statuses, default);
        
        // Asserts
        await getSubTasksAction.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task GetSubTasksInStatus_NoChildTasks_ReturnsEmptyArray()
    {
        // Arrange
        var parentTask = TaskEntityV1Faker.Generate();
        long parentTaskId = (await _repository.Add(parentTask, default)).First();
        
        var statuses = new TaskStatus[] { TaskStatus.ToDo, TaskStatus.InProgress };
        
        // Act
        var actualSubTasks = await _repository.GetSubTasksInStatus(parentTaskId, statuses, default);
        
        // Asserts
        actualSubTasks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TaskStatus.Done)]
    [InlineData(TaskStatus.Draft)]
    [InlineData(TaskStatus.ToDo)]
    [InlineData(TaskStatus.Draft, TaskStatus.ToDo)]
    [InlineData(TaskStatus.Done, TaskStatus.Canceled)]
    public async Task GetSubTasksInStatus_FilterByStatuses_ReturnsFilteredSubTasks(params TaskStatus[] statuses)
    {
        // Arrange
        long firstLayerTaskId = (await GenerateTasksInRepository(TaskStatus.InProgress)).First();
        long secondLayerTaskId = (await GenerateTasksInRepository(TaskStatus.InProgress, parentTaskId: firstLayerTaskId)).First();

        int subTasksCount = 5;
        var childStatuses = new[] { TaskStatus.Done, TaskStatus.Draft, TaskStatus.ToDo, TaskStatus.Canceled };
        foreach (var status in childStatuses)
            await GenerateTasksInRepository(status, count: subTasksCount, parentTaskId: secondLayerTaskId);

        var expectedParentTaskIds = new long[] { firstLayerTaskId, secondLayerTaskId };
            
        // Act
         var actualSubTasks = await _repository
             .GetSubTasksInStatus(firstLayerTaskId, statuses, default);

        // Asserts
        actualSubTasks.Should().OnlyContain(st => st.ParentTaskIds.SequenceEqual(expectedParentTaskIds));
        actualSubTasks.Should().OnlyContain(st => statuses.Contains(st.Status));
    }

    private async Task<long[]> GenerateTasksInRepository(TaskStatus status, int count = 1, long? parentTaskId = null)
    {
        var tasks = TaskEntityV1Faker.Generate(count);
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = tasks[i] with { ParentTaskId = parentTaskId, Status = (int)status };
        }
        var taskIds = await _repository.Add(tasks, default);
        return taskIds;
    }
}
