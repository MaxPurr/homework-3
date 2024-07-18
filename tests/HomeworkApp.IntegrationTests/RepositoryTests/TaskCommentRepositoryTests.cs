using FluentAssertions;
using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Exceptions;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.IntegrationTests.Creators;
using HomeworkApp.IntegrationTests.Fakers;
using HomeworkApp.IntegrationTests.Fixtures;
using HomeworkApp.Utils;
using Microsoft.VisualBasic.CompilerServices;
using Moq;
using Xunit;

namespace HomeworkApp.IntegrationTests.RepositoryTests;

[Collection(nameof(TestFixture))]
public class TaskCommentRepositoryTests
{
    private readonly ITaskCommentRepository _repository;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public TaskCommentRepositoryTests(TestFixture fixture)
    {
        _repository = fixture.TaskCommentRepository;
        _dateTimeProviderMock = fixture.DateTimeProviderMock;
    }

    [Fact]
    public async Task Add_Success()
    {
        // Arrange
        long taskId = 1;
        var taskComment = TaskCommentEntityV1Faker.Generate(taskId);
        
        // Act
        long taskCommentId = await _repository.Add(taskComment, default);
        var expectedTaskComment = taskComment.WithId(taskCommentId);
        
        var actualTaskComment = (await _repository.Get(new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = false,
            }, default))
            .FirstOrDefault(c => c.Id == taskCommentId);
        
        // Assert
        actualTaskComment.Should().NotBeNull();
        actualTaskComment.Should().BeEquivalentTo(expectedTaskComment);
    }

    [Fact]
    public async Task Update_TaskCommentNotFound_ThrowsRecordNotFoundException()
    {
        // Arrange
        long taskId = 1;
        long taskCommentId = 404;
        var taskComment = TaskCommentEntityV1Faker
            .Generate(taskId)
            .WithId(taskCommentId);
        
        // Act
        var updateAction = async() => await _repository.Update(taskComment, default);
        
        // Assert
        await updateAction.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task Update_TaskCommentExists_Success()
    {
        // Arrange
        long taskId = 1;
        var taskComment = TaskCommentEntityV1Faker.Generate(taskId);
        long taskCommentId = await _repository.Add(taskComment, default);

        DateTimeOffset modifiedAt = Create.RandomUtcTime();
        _dateTimeProviderMock
            .Setup(f => f.UtcNow)
            .Returns(modifiedAt);

         var updatedTaskComment = TaskCommentEntityV1Faker
             .Generate(taskId)
             .WithId(taskCommentId);
         var expectedMessage = updatedTaskComment.Message;
        
        // Act
        await _repository.Update(updatedTaskComment, default);
        var actual = (await _repository.Get(new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = false,
            }, default))
            .First(c => c.Id == taskCommentId);
        
        // Assert
        actual.ModifiedAt.Should().NotBeNull();
        actual.ModifiedAt.Should().BeCloseTo(modifiedAt, TimeSpan.FromSeconds(1));
        actual.Message.Should().Be(expectedMessage);
    }
    
    [Fact]
    public async Task SetDeleted_TaskCommentNotFound_ThrowsRecordNotFoundException()
    {
        // Arrange
        long taskCommentId = 404;
        
        // Act
        var setDeletedAction = async() => await _repository.SetDeleted(taskCommentId, default);
        
        // Assert
        await setDeletedAction.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task SetDeleted_TaskCommentExists_Success()
    {
        // Arrange
        long taskId = 1;
        var taskComment = TaskCommentEntityV1Faker.Generate(taskId);
        long taskCommentId = await _repository.Add(taskComment, default);
        
        DateTimeOffset deletedAt = Create.RandomUtcTime();
        _dateTimeProviderMock
            .Setup(f => f.UtcNow)
            .Returns(deletedAt);
        
        // Act
        await _repository.SetDeleted(taskCommentId, default);
        
        var actualTaskComment = (await _repository.Get(new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = true,
            }, default))
            .First(c => c.Id == taskCommentId);
        
        // Assert
        actualTaskComment.DeletedAt.Should().NotBeNull();
        actualTaskComment.DeletedAt.Should().BeCloseTo(deletedAt, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public async Task Get_IncludeDeleted_ReturnsAllTaskComments()
    {
        // Arrange
        long taskId = 1;
        var notDeletedComment = TaskCommentEntityV1Faker.Generate(taskId);
        long notDeletedCommentId = await _repository.Add(notDeletedComment, default);

        var deletedComment = TaskCommentEntityV1Faker
            .Generate(taskId);
        long deletedCommentId = await _repository.Add(deletedComment, default);
        await _repository.SetDeleted(deletedCommentId, default);
        
        // Act
        var comments = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = true
            }, default);
        
        // Assert
        comments.Should().Contain(x => x.Id == notDeletedCommentId);
        comments.Should().Contain(x => x.Id == deletedCommentId);
    }

    [Fact]
    public async Task Get_ExcludeDeleted_ReturnsNotDeletedTaskComments()
    {
        // Arrange
        long taskId = 1;
        var notDeletedComment = TaskCommentEntityV1Faker.Generate(taskId);
        long notDeletedCommentId = await _repository.Add(notDeletedComment, default);

        var deletedComment = TaskCommentEntityV1Faker
            .Generate(taskId);
        long deletedCommentId = await _repository.Add(deletedComment, default);
        await _repository.SetDeleted(deletedCommentId, default);
        
        // Act
        var comments = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = false
            }, default);
        
        // Assert
        comments.Should().Contain(x => x.Id == notDeletedCommentId);
        comments.Should().NotContain(x => x.Id == deletedCommentId);
    }

    [Fact]
    public async Task Get_TaskHasNoComments_ReturnsEmptyArray()
    {
        // Arrange
        long taskId = 2;
        
        // Act
        var comments = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = true
            }, default);
        
        // Assert
        comments.Should().BeEmpty();
    }
    
    [Fact]
    public async Task IsTaskCommentExist_TaskCommentDoesNotExist_ReturnsFalse()
    {
        // Arrange
        long taskCommentId = 404;
        bool expectedResult = false;

        // Act 
        bool actualResult = await _repository.IsTaskCommentExist(taskCommentId, default);

        // Asserts
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task IsTaskCommentExist_TaskCommentExists_ReturnsTrue()
    {
        // Arrange
        long taskId = 1;
        var taskComment = TaskCommentEntityV1Faker.Generate(taskId);
        long taskCommentId = await _repository.Add(taskComment, default);
        bool expectedResult = true;

        // Act 
        bool actualResult = await _repository.IsTaskCommentExist(taskCommentId, default);

        // Asserts
        actualResult.Should().Be(expectedResult);
    }
}