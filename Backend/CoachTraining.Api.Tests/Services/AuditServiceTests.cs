using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AuditServiceTests
{
    private static AuditService CreateService(Data.ApplicationDbContext db) => new(db);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<TrainingSession> SeedSessionAsync(Data.ApplicationDbContext db, Coach coach)
    {
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
            Status = SessionStatus.Scheduled,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task GetSessionAuditLogAsync_WhenNotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.GetSessionAuditLogAsync(999, isPrivilegedRole: true, currentCoachId: null);

        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task GetSessionAuditLogAsync_ByOtherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.GetSessionAuditLogAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: otherCoach.CoachId);

        Assert.True(result.Forbidden);
    }

    [Fact]
    public async Task GetSessionAuditLogAsync_ByOwningCoach_ReturnsOrderedEntries()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        db.AuditLogs.AddRange(
            new AuditLog { EntityName = nameof(TrainingSession), EntityId = session.TrainingSessionId, Action = "Cancel", ActionByUserId = 1, ActionDate = new DateTime(2026, 1, 1) },
            new AuditLog { EntityName = nameof(TrainingSession), EntityId = session.TrainingSessionId, Action = "Reschedule", ActionByUserId = 1, ActionDate = new DateTime(2026, 1, 2) });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetSessionAuditLogAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId);

        Assert.False(result.NotFound);
        Assert.False(result.Forbidden);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("Reschedule", result.Data[0].Action); // most recent first
    }

    [Fact]
    public async Task GetApprovalHistoryAsync_ReturnsRecordedActions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        db.TrainingApprovalHistories.Add(new TrainingApprovalHistory
        {
            TrainingSessionId = session.TrainingSessionId,
            ActionType = ApprovalActionType.Submit,
            ActionByUserId = 1,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetApprovalHistoryAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null);

        var entry = Assert.Single(result.Data!);
        Assert.Equal(ApprovalActionType.Submit, entry.ActionType);
    }

    [Fact]
    public async Task GetSubstitutionHistoryAsync_ResolvesCoachCodeAndName()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, originalCoach);
        db.CoachSubstitutionHistories.Add(new CoachSubstitutionHistory
        {
            TrainingSessionId = session.TrainingSessionId,
            OriginalCoachId = originalCoach.CoachId,
            SubstituteCoachId = substituteCoach.CoachId,
            Reason = "แทนชั่วคราว",
            ActionByUserId = 1,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetSubstitutionHistoryAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null);

        var entry = Assert.Single(result.Data!);
        Assert.Equal(substituteCoach.CoachCode, entry.SubstituteCoachCode);
        Assert.Equal(originalCoach.FullName, entry.OriginalCoachName);
    }

    [Fact]
    public async Task GetConflictOverrideHistoryAsync_ReturnsRecordedOverrides()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        db.ConflictOverrideHistories.Add(new ConflictOverrideHistory
        {
            TrainingSessionId = session.TrainingSessionId,
            ConflictType = ConflictType.CoachOverlap,
            Reason = "ผู้บริหารอนุมัติ",
            ActionByUserId = 1,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetConflictOverrideHistoryAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null);

        var entry = Assert.Single(result.Data!);
        Assert.Equal(ConflictType.CoachOverlap, entry.ConflictType);
        Assert.Equal("ผู้บริหารอนุมัติ", entry.Reason);
    }
}
