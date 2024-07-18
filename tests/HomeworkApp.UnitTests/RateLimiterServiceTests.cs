using FluentAssertions;
using HomeworkApp.Bll.Services;
using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Repositories.Interfaces;
using Moq;

namespace HomeworkApp.UnitTests;

public class RateLimiterServiceTests
{
    private readonly IRateLimiterService _rateLimiterService;
    private readonly Mock<IRateLimitRepository> _rateLimitRepositoryMock;
        
    public RateLimiterServiceTests()
    {
        _rateLimitRepositoryMock = new Mock<IRateLimitRepository>(MockBehavior.Loose);  
        _rateLimiterService = new RateLimiterService(_rateLimitRepositoryMock.Object);
    }

    [Fact]
    public async Task IsRateLimitExceeded_RemainingRequestsAreLessThanOrEqualToZero_ReturnsTrue()
    {
        // Arrange
        string userIP = "0.0.0.0";
        long remainingRequests = 0;
        _rateLimitRepositoryMock
            .Setup(f => f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default))
            .ReturnsAsync(false);

        _rateLimitRepositoryMock
            .Setup(f => f.DecrementRemainingRequests(userIP, default))
            .ReturnsAsync(remainingRequests);

        bool expectedResult = true;

        // Act
        bool actualResult = await _rateLimiterService.IsRateLimitExceeded(userIP, default);

        // Assert
        actualResult.Should().Be(expectedResult);
        _rateLimitRepositoryMock.Verify(f => 
            f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default), 
            Times.Once);
        _rateLimitRepositoryMock.Verify(f => 
            f.DecrementRemainingRequests(userIP, default),
            Times.Once);
    }

    [Fact]
    public async Task IsRateLimitExceeded_RemainingRequestsAreGreaterThanZero_ReturnsFalse()
    {
        // Arrange
        string userIP = "0.0.0.0";
        long remainingRequests = 1;
        _rateLimitRepositoryMock
            .Setup(f => f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default))
            .ReturnsAsync(false);

        _rateLimitRepositoryMock
            .Setup(f => f.DecrementRemainingRequests(userIP, default))
            .ReturnsAsync(remainingRequests);

        bool expectedResult = false;

        // Act
        bool actualResult = await _rateLimiterService.IsRateLimitExceeded(userIP, default);

        // Assert
        actualResult.Should().Be(expectedResult);
        _rateLimitRepositoryMock.Verify(f => 
                f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default), 
            Times.Once);
        _rateLimitRepositoryMock.Verify(f => 
                f.DecrementRemainingRequests(userIP, default),
            Times.Once);
    }

    [Fact]
    public async Task IsRateLimitExceeded_UserIPDoesNotExist_ReturnsFalse()
    {
        // Arrange
        string userIP = "0.0.0.0";
        
        _rateLimitRepositoryMock
            .Setup(f => f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default))
            .ReturnsAsync(true);
        
        bool expectedResult = false;
        
        // Act 
        bool actualResult = await _rateLimiterService.IsRateLimitExceeded(userIP, default);
        
        // Assert
        actualResult.Should().Be(expectedResult);
        _rateLimitRepositoryMock.Verify(f => 
                f.SetRequestsPerMinuteIfNotExists(userIP, It.IsAny<long>(), default), 
            Times.Once);
        _rateLimitRepositoryMock.Verify(f => 
                f.DecrementRemainingRequests(userIP, default),
            Times.Never);
    }
}