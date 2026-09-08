using CoachTraining.Api.DTOs.Approvals;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class TrainingApprovalServiceTests
{
    private static TrainingApprovalService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<TrainingApprovalService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<Athlete> SeedAthleteAsync(Data.ApplicationDbContext db, string code = "A001")
    {
        var athlete = new Athlete { AthleteCode = code, FullName = $"Athlete {code}", IsActive = true };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync();
        return athlete;
    }

    private static async Task<TrainingSession> SeedSessionAsync(
        Data.ApplicationDbContext db,
        Coach assignedCoach,
        SessionStatus status = SessionStatus.Completed,
        TrainingType trainingType = TrainingType.Routine)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = trainingType,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            ActualStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ActualEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            AssignedCoachId = assignedCoach.CoachId,
            AssignedCoachCodeSnapshot = assignedCoach.CoachCode,
            AssignedCoachNameSnapshot = assignedCoach.FullName,
            Status = status,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task SubmitAsync_ByAssignedCoach_TransitionsToSubmittedAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.SubmitAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.False(result.NotFound);
        Assert.False(result.Forbidden);
        Assert.Null(result.Error);
        Assert.Equal(SessionStatus.Submitted, result.Session!.Status);
        Assert.Equal(ApprovalActionType.Submit, result.History!.ActionType);

        var history = await db.TrainingApprovalHistories.SingleAsync();
        Assert.Equal(ApprovalActionType.Submit, history.ActionType);
        Assert.Equal(1, history.ActionByUserId);
    }

    [Fact]
    public async Task SubmitAsync_ByOtherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.SubmitAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: otherCoach.CoachId, actionByUserId: 1);

        Assert.True(result.Forbidden);
        Assert.Empty(db.TrainingApprovalHistories);
    }

    [Fact]
    public async Task SubmitAsync_WhenNotCompleted_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Scheduled);
        var service = CreateService(db);

        var result = await service.SubmitAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task SubmitAsync_PrivateSessionWithIncompleteAttendance_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach, trainingType: TrainingType.Private);
        session.PrivateAthletes.Add(new PrivateSessionAthlete
        {
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SubmitAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task SubmitAsync_PrivateSessionWithFullAttendance_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach, trainingType: TrainingType.Private);
        session.PrivateAthletes.Add(new PrivateSessionAthlete
        {
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
        });
        db.Attendances.Add(new Attendance
        {
            TrainingSessionId = session.TrainingSessionId,
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
            Status = AttendanceStatus.Present,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SubmitAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.Equal(SessionStatus.Submitted, result.Session!.Status);
    }

    [Fact]
    public async Task ApproveAsync_LocksSessionAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Submitted);
        var service = CreateService(db);

        var result = await service.ApproveAsync(session.TrainingSessionId, new ApprovalCommentRequest { Reason = "เรียบร้อย" }, actionByUserId: 9);

        Assert.Null(result.Error);
        // FR-APPROVAL-003 — approving locks the record immediately; there is no
        // separate intermediate "Approved" state exposed to callers.
        Assert.Equal(SessionStatus.Locked, result.Session!.Status);
        Assert.Equal(ApprovalActionType.Approve, result.History!.ActionType);

        var history = await db.TrainingApprovalHistories.SingleAsync();
        Assert.Equal(ApprovalActionType.Approve, history.ActionType);
        Assert.Equal("เรียบร้อย", history.Reason);
        Assert.Equal(9, history.ActionByUserId);
    }

    [Fact]
    public async Task ApproveAsync_WhenNotSubmitted_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Completed);
        var service = CreateService(db);

        var result = await service.ApproveAsync(session.TrainingSessionId, new ApprovalCommentRequest(), actionByUserId: 9);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task RejectAsync_ReturnsSessionToCompletedAndRequiresReason()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Submitted);
        var service = CreateService(db);

        var missingReason = await service.RejectAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "   " }, actionByUserId: 9);
        Assert.NotNull(missingReason.Error);
        Assert.Equal(SessionStatus.Submitted, (await db.TrainingSessions.FindAsync(session.TrainingSessionId))!.Status);

        var result = await service.RejectAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "ข้อมูลไม่ครบ" }, actionByUserId: 9);

        Assert.Null(result.Error);
        Assert.Equal(SessionStatus.Completed, result.Session!.Status);
        Assert.Equal(ApprovalActionType.Reject, result.History!.ActionType);
        Assert.Equal("ข้อมูลไม่ครบ", result.History.Reason);
    }

    [Fact]
    public async Task RequestRevisionAsync_ReturnsSessionToCompletedWithDistinctActionType()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Submitted);
        var service = CreateService(db);

        var result = await service.RequestRevisionAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "กรุณาแก้ไขบันทึกการฝึก" }, actionByUserId: 9);

        Assert.Null(result.Error);
        Assert.Equal(SessionStatus.Completed, result.Session!.Status);
        Assert.Equal(ApprovalActionType.RequestRevision, result.History!.ActionType);
    }

    [Fact]
    public async Task UnlockAsync_ReturnsLockedSessionToCompletedAndRequiresReason()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Locked);
        var service = CreateService(db);

        var missingReason = await service.UnlockAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "" }, actionByUserId: 9);
        Assert.NotNull(missingReason.Error);

        var result = await service.UnlockAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "แก้ไขจำนวนนักกีฬาที่เข้าร่วม" }, actionByUserId: 9);

        Assert.Null(result.Error);
        Assert.Equal(SessionStatus.Completed, result.Session!.Status);
        Assert.Equal(ApprovalActionType.Unlock, result.History!.ActionType);
        Assert.Equal("แก้ไขจำนวนนักกีฬาที่เข้าร่วม", result.History.Reason);
    }

    [Fact]
    public async Task UnlockAsync_WhenNotLocked_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Completed);
        var service = CreateService(db);

        var result = await service.UnlockAsync(session.TrainingSessionId, new ApprovalReasonRequest { Reason = "แก้ไข" }, actionByUserId: 9);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task SubmitAsync_WhenSessionNotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.SubmitAsync(999, isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.True(result.NotFound);
    }
}
