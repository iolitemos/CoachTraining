using System.Text.Json;
using CoachTraining.Api.DTOs.Cancellations;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CancellationServiceTests
{
    private static CancellationService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<CancellationService>.Instance);

    private static async Task<TrainingSession> SeedSessionAsync(
        Data.ApplicationDbContext db,
        SessionStatus status = SessionStatus.Scheduled)
    {
        var coach = new Coach { CoachCode = "C001", FullName = "Coach C001", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = status,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.InProgress)]
    [InlineData(SessionStatus.CoachAbsent)]
    public async Task CancelAsync_FromEligibleStatus_CancelsAndRecordsAudit(SessionStatus initialStatus)
    {
        using var db = TestDbContextFactory.Create();
        var session = await SeedSessionAsync(db, initialStatus);
        var service = CreateService(db);

        var result = await service.CancelAsync(
            session.TrainingSessionId,
            new CancelSessionRequest { Reason = "สถานที่ไม่พร้อมใช้งาน" },
            actionByUserId: 42);

        Assert.False(result.NotFound);
        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.Equal(SessionStatus.Cancelled, result.Session!.Status);
        Assert.Equal("สถานที่ไม่พร้อมใช้งาน", result.Session.CancellationReason);

        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal(nameof(TrainingSession), audit.EntityName);
        Assert.Equal(session.TrainingSessionId, audit.EntityId);
        Assert.Equal("Cancel", audit.Action);
        using var previousValue = JsonDocument.Parse(audit.PreviousValue!);
        using var newValue = JsonDocument.Parse(audit.NewValue!);
        Assert.Equal(initialStatus.ToString(), previousValue.RootElement.GetProperty("Status").GetString());
        Assert.Equal(nameof(SessionStatus.Cancelled), newValue.RootElement.GetProperty("Status").GetString());
        Assert.Equal("สถานที่ไม่พร้อมใช้งาน", newValue.RootElement.GetProperty("CancellationReason").GetString());
        Assert.Equal(42, audit.ActionByUserId);
    }

    [Theory]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Submitted)]
    [InlineData(SessionStatus.Approved)]
    [InlineData(SessionStatus.Locked)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Rescheduled)]
    public async Task CancelAsync_FromIneligibleStatus_ReturnsErrorWithoutAudit(SessionStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var session = await SeedSessionAsync(db, status);
        var service = CreateService(db);

        var result = await service.CancelAsync(
            session.TrainingSessionId,
            new CancelSessionRequest { Reason = "ยกเลิกเซสชัน" },
            actionByUserId: 42);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
        Assert.Equal(status, session.Status);
        Assert.Null(session.CancellationReason);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CancelAsync_WithWhitespaceReason_ReturnsErrorWithoutChangingSession()
    {
        using var db = TestDbContextFactory.Create();
        var session = await SeedSessionAsync(db);
        var service = CreateService(db);

        var result = await service.CancelAsync(
            session.TrainingSessionId,
            new CancelSessionRequest { Reason = "   " },
            actionByUserId: 42);

        Assert.NotNull(result.Error);
        Assert.Equal(SessionStatus.Scheduled, session.Status);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CancelAsync_CancelledSessionRemainsAvailableInHistoricalQuery()
    {
        using var db = TestDbContextFactory.Create();
        var session = await SeedSessionAsync(db);
        var service = CreateService(db);
        await service.CancelAsync(
            session.TrainingSessionId,
            new CancelSessionRequest { Reason = "ยกเลิกเซสชัน" },
            actionByUserId: 42);

        var queryService = new TrainingSessionService(db);
        var result = await queryService.GetByIdAsync(
            session.TrainingSessionId,
            isPrivilegedRole: true,
            currentCoachId: null);

        Assert.NotNull(result);
        Assert.Equal(SessionStatus.Cancelled, result!.Status);
        Assert.Equal("ยกเลิกเซสชัน", result.CancellationReason);
    }

    [Fact]
    public async Task CancelAsync_NonExistentSession_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.CancelAsync(
            999,
            new CancelSessionRequest { Reason = "ยกเลิกเซสชัน" },
            actionByUserId: 42);

        Assert.True(result.NotFound);
        Assert.Null(result.Session);
        Assert.Empty(db.AuditLogs);
    }
}
