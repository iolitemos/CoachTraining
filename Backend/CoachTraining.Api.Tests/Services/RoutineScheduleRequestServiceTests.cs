using CoachTraining.Api.DTOs.RoutineScheduleRequests;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class RoutineScheduleRequestServiceTests
{
    private static RoutineScheduleRequestService CreateService(Data.ApplicationDbContext db)
    {
        var conflicts = new ScheduleConflictService(db);
        var schedules = new RoutineScheduleService(db, conflicts, NullLogger<RoutineScheduleService>.Instance);
        return new RoutineScheduleRequestService(db, conflicts, schedules);
    }

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db)
    {
        var coach = new Coach { CoachCode = "C-REQUEST", FullName = "Request Coach", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    [Fact]
    public async Task CoachRequest_DoesNotCreateOperationalScheduleBeforeApproval()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var result = await CreateService(db).CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);

        Assert.Null(result.Error);
        Assert.Equal(RoutineScheduleRequestStatus.Pending, result.Request!.Status);
        Assert.Empty(await db.RoutineSchedules.ToListAsync());
        Assert.Empty(await db.TrainingSessions.ToListAsync());
    }

    [Fact]
    public async Task Approve_PreservesProposalAndCreatesScheduleFromAdminValues()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var created = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
            ProposedRemarks = "ค่าที่โค้ชเสนอ",
        }, 10);

        var result = await service.ApproveAsync(created.Request!.RoutineScheduleRequestId, new RoutineScheduleRequestReviewDto
        {
            ApprovedDate = new DateOnly(2026, 11, 3),
            ApprovedStartTime = new TimeOnly(17, 30),
            ApprovedEndTime = new TimeOnly(19, 30),
            ApprovedRemarks = "ค่าที่แอดมินยืนยัน",
        }, 20);

        Assert.Null(result.Error);
        Assert.Equal(RoutineScheduleRequestStatus.Approved, result.Request!.Status);
        Assert.Equal(new DateOnly(2026, 11, 2), result.Request.ProposedDate);
        Assert.Equal(new DateOnly(2026, 11, 3), result.Request.ApprovedDate);
        var schedule = await db.RoutineSchedules.SingleAsync();
        Assert.Equal(new DateOnly(2026, 11, 3), schedule.EffectiveStartDate);
        Assert.Equal(new TimeOnly(17, 30), schedule.StartTime);
        Assert.Single(await db.TrainingSessions.ToListAsync());
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.EntityName == nameof(RoutineScheduleRequest) && x.Action == "Approve");
    }

    [Fact]
    public async Task Reject_RecordsReasonAndCreatesNoSchedule()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var created = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);

        var result = await service.RejectAsync(created.Request!.RoutineScheduleRequestId,
            new RoutineScheduleRequestDecisionDto { Reason = "วันดังกล่าวปิดสนาม" }, 20);

        Assert.Equal(RoutineScheduleRequestStatus.Rejected, result.Request!.Status);
        Assert.Equal("วันดังกล่าวปิดสนาม", result.Request.ReviewReason);
        Assert.Empty(await db.RoutineSchedules.ToListAsync());
    }

    [Fact]
    public async Task Revision_CanBeEditedByOwningCoachAndReturnsToPending()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var created = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2), ProposedStartTime = new TimeOnly(18, 0), ProposedEndTime = new TimeOnly(20, 0),
        }, 10);
        await service.RequestRevisionAsync(created.Request!.RoutineScheduleRequestId,
            new RoutineScheduleRequestDecisionDto { Reason = "กรุณาเลื่อนเวลา" }, 20);

        var updated = await service.UpdateOwnAsync(created.Request.RoutineScheduleRequestId, coach.CoachId,
            new RoutineScheduleRequestCreateDto
            {
                ProposedDate = new DateOnly(2026, 11, 2), ProposedStartTime = new TimeOnly(19, 0), ProposedEndTime = new TimeOnly(21, 0),
            }, 10);

        Assert.Equal(RoutineScheduleRequestStatus.Pending, updated.Request!.Status);
        Assert.Equal(new TimeOnly(19, 0), updated.Request.ProposedStartTime);
        Assert.Null(updated.Request.ReviewReason);
    }

    [Fact]
    public async Task ListOwn_FiltersRequestsToVisibleCalendarRange()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        foreach (var date in new[] { new DateOnly(2026, 10, 31), new DateOnly(2026, 11, 15), new DateOnly(2026, 12, 1) })
        {
            await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
            {
                ProposedDate = date,
                ProposedStartTime = new TimeOnly(18, 0),
                ProposedEndTime = new TimeOnly(20, 0),
            }, 10);
        }

        var requests = await service.ListOwnAsync(coach.CoachId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Single(requests);
        Assert.Equal(new DateOnly(2026, 11, 15), requests[0].ProposedDate);
    }

    [Fact]
    public async Task AdministratorList_WithoutStatus_ReturnsPendingAndApprovedRequests()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var approved = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);
        await service.ApproveAsync(approved.Request!.RoutineScheduleRequestId, new RoutineScheduleRequestReviewDto
        {
            ApprovedDate = approved.Request.ProposedDate,
            ApprovedStartTime = approved.Request.ProposedStartTime,
            ApprovedEndTime = approved.Request.ProposedEndTime,
        }, 20);
        var replacementCoach = new Coach
        {
            CoachCode = "C-REPLACEMENT",
            FullName = "Replacement Coach",
            Nickname = "New",
            ColorHex = "#2563EB",
            IsActive = true,
        };
        db.Coaches.Add(replacementCoach);
        await db.SaveChangesAsync();
        var generatedSchedule = await db.RoutineSchedules.SingleAsync();
        generatedSchedule.CoachId = replacementCoach.CoachId;
        generatedSchedule.Coach = replacementCoach;
        generatedSchedule.EffectiveStartDate = new DateOnly(2026, 11, 5);
        generatedSchedule.StartTime = new TimeOnly(19, 0);
        await db.SaveChangesAsync();
        await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 4),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);

        var requests = await service.ListAsync(null);

        Assert.Equal(2, requests.Count);
        Assert.Contains(requests, request => request.Status == RoutineScheduleRequestStatus.Pending);
        var approvedRequest = Assert.Single(requests, request => request.Status == RoutineScheduleRequestStatus.Approved);
        Assert.Equal(new DateOnly(2026, 11, 2), approvedRequest.ApprovedDate);
        Assert.Equal(coach.CoachId, approvedRequest.CoachId);
        Assert.Equal("#10B981", approvedRequest.CoachColorHex);
        Assert.Equal(replacementCoach.CoachId, approvedRequest.CurrentScheduleCoachId);
        Assert.Equal("C-REPLACEMENT", approvedRequest.CurrentScheduleCoachCode);
        Assert.Equal("Replacement Coach", approvedRequest.CurrentScheduleCoachName);
        Assert.Equal("New", approvedRequest.CurrentScheduleCoachNickname);
        Assert.Equal("#2563EB", approvedRequest.CurrentScheduleCoachColorHex);
        Assert.Equal(new DateOnly(2026, 11, 5), approvedRequest.CurrentScheduleDate);
        Assert.Equal(new TimeOnly(19, 0), approvedRequest.CurrentScheduleStartTime);
    }

    [Fact]
    public async Task ApproveBatch_UsesCoachProposalsAndCreatesEverySelectedSchedule()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var first = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
            ProposedRemarks = "วันแรก",
        }, 10);
        var second = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 4),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
            ProposedRemarks = "วันที่สอง",
        }, 10);

        var result = await service.ApproveBatchAsync(new RoutineScheduleRequestBatchApproveDto
        {
            RequestIds = [first.Request!.RoutineScheduleRequestId, second.Request!.RoutineScheduleRequestId],
        }, 20);

        Assert.Null(result.Error);
        Assert.Equal(2, result.Requests.Count);
        Assert.All(result.Requests, request =>
        {
            Assert.Equal(RoutineScheduleRequestStatus.Approved, request.Status);
            Assert.Equal(request.ProposedDate, request.ApprovedDate);
            Assert.Equal(request.ProposedStartTime, request.ApprovedStartTime);
            Assert.NotNull(request.RoutineScheduleId);
        });
        Assert.Equal(2, await db.RoutineSchedules.CountAsync());
        Assert.Equal(2, await db.TrainingSessions.CountAsync());
        Assert.Equal(2, await db.AuditLogs.CountAsync(log =>
            log.EntityName == nameof(RoutineScheduleRequest) && log.Action == "BatchApprove"));
    }

    [Fact]
    public async Task ApproveBatch_RejectsSelectionContainingNonPendingRequestBeforeCreatingSchedules()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var approved = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 2),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);
        await service.ApproveAsync(approved.Request!.RoutineScheduleRequestId, new RoutineScheduleRequestReviewDto
        {
            ApprovedDate = approved.Request.ProposedDate,
            ApprovedStartTime = approved.Request.ProposedStartTime,
            ApprovedEndTime = approved.Request.ProposedEndTime,
        }, 20);
        var pending = await service.CreateAsync(coach.CoachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = new DateOnly(2026, 11, 4),
            ProposedStartTime = new TimeOnly(18, 0),
            ProposedEndTime = new TimeOnly(20, 0),
        }, 10);

        var result = await service.ApproveBatchAsync(new RoutineScheduleRequestBatchApproveDto
        {
            RequestIds = [approved.Request.RoutineScheduleRequestId, pending.Request!.RoutineScheduleRequestId],
        }, 20);

        Assert.Contains("รอตรวจสอบ", result.Error);
        Assert.Single(await db.RoutineSchedules.ToListAsync());
        Assert.Equal(RoutineScheduleRequestStatus.Pending,
            (await db.RoutineScheduleRequests.FindAsync(pending.Request.RoutineScheduleRequestId))!.Status);
    }
}
