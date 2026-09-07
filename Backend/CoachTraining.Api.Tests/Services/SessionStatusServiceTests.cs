using CoachTraining.Api.Services;
using SessionStatus = CoachTraining.Api.Models.Enums.SessionStatus;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class SessionStatusServiceTests
{
    private readonly SessionStatusService _service = new();

    [Theory]
    [InlineData(SessionStatus.Scheduled, SessionStatus.InProgress)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Rescheduled)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.CoachAbsent)]
    [InlineData(SessionStatus.InProgress, SessionStatus.Completed)]
    [InlineData(SessionStatus.InProgress, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.CoachAbsent, SessionStatus.InProgress)]
    [InlineData(SessionStatus.Completed, SessionStatus.Submitted)]
    [InlineData(SessionStatus.Submitted, SessionStatus.Approved)]
    [InlineData(SessionStatus.Submitted, SessionStatus.Completed)]
    [InlineData(SessionStatus.Approved, SessionStatus.Locked)]
    [InlineData(SessionStatus.Locked, SessionStatus.Completed)]
    [InlineData(SessionStatus.Cancelled, SessionStatus.Scheduled)]
    public void CanTransition_AllowsExpectedTransitions(SessionStatus from, SessionStatus to)
    {
        Assert.True(_service.CanTransition(from, to));
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Completed)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Approved)]
    [InlineData(SessionStatus.Cancelled, SessionStatus.Completed)]
    [InlineData(SessionStatus.Cancelled, SessionStatus.InProgress)]
    [InlineData(SessionStatus.Locked, SessionStatus.Approved)]
    [InlineData(SessionStatus.Locked, SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Rescheduled, SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Approved, SessionStatus.Submitted)]
    public void CanTransition_RejectsInvalidTransitions(SessionStatus from, SessionStatus to)
    {
        Assert.False(_service.CanTransition(from, to));
    }

    [Fact]
    public void EnsureValidTransition_WithInvalidTransition_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _service.EnsureValidTransition(SessionStatus.Cancelled, SessionStatus.Completed));
    }

    [Fact]
    public void EnsureValidTransition_WithValidTransition_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.EnsureValidTransition(SessionStatus.Scheduled, SessionStatus.InProgress));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled, true)]
    [InlineData(SessionStatus.InProgress, true)]
    [InlineData(SessionStatus.Completed, true)]
    [InlineData(SessionStatus.Submitted, false)]
    [InlineData(SessionStatus.Approved, false)]
    [InlineData(SessionStatus.Locked, false)]
    [InlineData(SessionStatus.Cancelled, false)]
    [InlineData(SessionStatus.Rescheduled, false)]
    [InlineData(SessionStatus.CoachAbsent, false)]
    public void IsEditableByCoach_MatchesExpectedStatuses(SessionStatus status, bool expected)
    {
        Assert.Equal(expected, _service.IsEditableByCoach(status));
    }

    [Theory]
    [InlineData(SessionStatus.Completed, true)]
    [InlineData(SessionStatus.Submitted, true)]
    [InlineData(SessionStatus.Approved, true)]
    [InlineData(SessionStatus.Locked, true)]
    [InlineData(SessionStatus.Scheduled, false)]
    [InlineData(SessionStatus.InProgress, false)]
    [InlineData(SessionStatus.Cancelled, false)]
    [InlineData(SessionStatus.Rescheduled, false)]
    [InlineData(SessionStatus.CoachAbsent, false)]
    public void CountsAsCompletedTeaching_MatchesExpectedStatuses(SessionStatus status, bool expected)
    {
        Assert.Equal(expected, _service.CountsAsCompletedTeaching(status));
    }
}
