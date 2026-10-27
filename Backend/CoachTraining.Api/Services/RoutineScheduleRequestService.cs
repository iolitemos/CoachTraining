using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.RoutineScheduleRequests;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoachTraining.Api.Services;

public class RoutineScheduleRequestService : IRoutineScheduleRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IScheduleConflictService _conflictService;
    private readonly IRoutineScheduleService _routineScheduleService;

    public RoutineScheduleRequestService(ApplicationDbContext db, IScheduleConflictService conflictService, IRoutineScheduleService routineScheduleService)
    {
        _db = db;
        _conflictService = conflictService;
        _routineScheduleService = routineScheduleService;
    }

    public async Task<RoutineScheduleRequestActionResult> CreateAsync(int coachId, RoutineScheduleRequestCreateDto dto, int userId)
    {
        var validation = await ValidateProposalAsync(coachId, dto.ProposedDate, dto.ProposedStartTime, dto.ProposedEndTime);
        if (validation.Error is not null) return validation;

        var request = new RoutineScheduleRequest
        {
            SubmissionBatchId = Guid.NewGuid(),
            CoachId = coachId,
            ProposedDate = dto.ProposedDate,
            ProposedStartTime = dto.ProposedStartTime,
            ProposedEndTime = dto.ProposedEndTime,
            ProposedRemarks = dto.ProposedRemarks?.Trim(),
            CreatedByUserId = userId,
        };
        _db.RoutineScheduleRequests.Add(request);
        await _db.SaveChangesAsync();
        AddAudit(request.RoutineScheduleRequestId, "Submit", null, ProposalJson(request), userId);
        await _db.SaveChangesAsync();
        await _db.Entry(request).Reference(x => x.Coach).LoadAsync();
        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    public async Task<(List<RoutineScheduleRequestDto> Requests, string? Error, List<string> Conflicts)> CreateBatchAsync(
        int coachId, RoutineScheduleRequestBatchCreateDto dto, int userId)
    {
        var selectedDays = dto.DaysOfWeek.ToHashSet();
        var dates = Enumerable.Range(0, dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1)
            .Select(dto.StartDate.AddDays)
            .Where(date => selectedDays.Contains(date.DayOfWeek))
            .ToList();
        if (dates.Count == 0) return ([], "ไม่พบวันที่ตรงกับวันในสัปดาห์ที่เลือก", []);

        var conflicts = new List<string>();
        foreach (var date in dates)
        {
            var validation = await ValidateProposalAsync(coachId, date, dto.StartTime, dto.EndTime);
            if (validation.Error is not null) conflicts.AddRange(validation.Conflicts.Count > 0 ? validation.Conflicts : [validation.Error]);
        }
        if (conflicts.Count > 0) return ([], "พบตารางหรือคำขอของโค้ชทับซ้อนกัน", conflicts.Distinct().ToList());

        var coach = await _db.Coaches.FirstAsync(x => x.CoachId == coachId);
        var batchId = Guid.NewGuid();
        var requests = dates.Select(date => new RoutineScheduleRequest
        {
            SubmissionBatchId = batchId,
            CoachId = coachId,
            Coach = coach,
            ProposedDate = date,
            ProposedStartTime = dto.StartTime,
            ProposedEndTime = dto.EndTime,
            ProposedRemarks = dto.Remarks?.Trim(),
            CreatedByUserId = userId,
        }).ToList();
        _db.RoutineScheduleRequests.AddRange(requests);
        await _db.SaveChangesAsync();
        foreach (var request in requests) AddAudit(request.RoutineScheduleRequestId, "Submit", null, ProposalJson(request), userId);
        await _db.SaveChangesAsync();
        return (requests.Select(Map).ToList(), null, []);
    }

    public async Task<List<RoutineScheduleRequestDto>> ListOwnAsync(int coachId, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var query = Query().Where(x => x.CoachId == coachId);
        if (dateFrom is not null) query = query.Where(x => x.ProposedDate >= dateFrom);
        if (dateTo is not null) query = query.Where(x => x.ProposedDate <= dateTo);
        return (await query.OrderByDescending(x => x.CreatedDate).ToListAsync()).Select(Map).ToList();
    }

    public async Task<RoutineScheduleRequestActionResult> UpdateOwnAsync(int id, int coachId, RoutineScheduleRequestCreateDto dto, int userId)
    {
        var request = await Query().FirstOrDefaultAsync(x => x.RoutineScheduleRequestId == id && x.CoachId == coachId);
        if (request is null) return new RoutineScheduleRequestActionResult { NotFound = true };
        if (request.Status is not (RoutineScheduleRequestStatus.Pending or RoutineScheduleRequestStatus.RevisionRequested))
            return new RoutineScheduleRequestActionResult { Error = "แก้ไขได้เฉพาะคำขอที่รอตรวจสอบหรือถูกขอให้แก้ไข" };
        var validation = await ValidateProposalAsync(coachId, dto.ProposedDate, dto.ProposedStartTime, dto.ProposedEndTime, id);
        if (validation.Error is not null) return validation;

        var previous = ProposalJson(request);
        request.ProposedDate = dto.ProposedDate;
        request.ProposedStartTime = dto.ProposedStartTime;
        request.ProposedEndTime = dto.ProposedEndTime;
        request.ProposedRemarks = dto.ProposedRemarks?.Trim();
        request.Status = RoutineScheduleRequestStatus.Pending;
        request.ReviewReason = null;
        request.ReviewedByUserId = null;
        request.ReviewedDate = null;
        request.UpdatedByUserId = userId;
        request.UpdatedDate = DateTime.UtcNow;
        AddAudit(request.RoutineScheduleRequestId, "ReviseAndResubmit", previous, ProposalJson(request), userId);
        await _db.SaveChangesAsync();
        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    public async Task<RoutineScheduleRequestActionResult> CancelOwnAsync(int id, int coachId, int userId)
    {
        var request = await Query().FirstOrDefaultAsync(x => x.RoutineScheduleRequestId == id && x.CoachId == coachId);
        if (request is null) return new RoutineScheduleRequestActionResult { NotFound = true };
        if (request.Status is not (RoutineScheduleRequestStatus.Pending or RoutineScheduleRequestStatus.RevisionRequested))
            return new RoutineScheduleRequestActionResult { Error = "ยกเลิกได้เฉพาะคำขอที่ยังไม่สิ้นสุด" };
        var previous = ProposalJson(request);
        request.Status = RoutineScheduleRequestStatus.Cancelled;
        request.UpdatedByUserId = userId;
        request.UpdatedDate = DateTime.UtcNow;
        AddAudit(request.RoutineScheduleRequestId, "Cancel", previous, JsonSerializer.Serialize(new { Status = request.Status.ToString() }), userId);
        await _db.SaveChangesAsync();
        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    public async Task<List<RoutineScheduleRequestDto>> ListAsync(RoutineScheduleRequestStatus? status)
    {
        var query = Query();
        if (status is not null) query = query.Where(x => x.Status == status);
        return (await query.OrderBy(x => x.Status).ThenBy(x => x.ProposedDate).ThenBy(x => x.ProposedStartTime)
            .ToListAsync()).Select(Map).ToList();
    }

    public async Task<RoutineScheduleRequestActionResult> ApproveAsync(int id, RoutineScheduleRequestReviewDto dto, int userId)
    {
        var request = await Query().FirstOrDefaultAsync(x => x.RoutineScheduleRequestId == id);
        if (request is null) return new RoutineScheduleRequestActionResult { NotFound = true };
        if (request.Status != RoutineScheduleRequestStatus.Pending)
            return new RoutineScheduleRequestActionResult { Error = "อนุมัติได้เฉพาะคำขอที่กำลังรอตรวจสอบ" };
        var previous = ProposalJson(request);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var save = await _routineScheduleService.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = request.CoachId,
            EffectiveStartDate = dto.ApprovedDate,
            StartTime = dto.ApprovedStartTime,
            EndTime = dto.ApprovedEndTime,
            Remarks = dto.ApprovedRemarks,
            OverrideConflict = dto.OverrideConflict,
            OverrideReason = dto.OverrideReason,
        }, userId);
        if (save.Error is not null)
            return new RoutineScheduleRequestActionResult { Error = save.Error, Conflicts = save.Conflicts.Select(x => x.Message).ToList() };

        request.Status = RoutineScheduleRequestStatus.Approved;
        request.ApprovedDate = dto.ApprovedDate;
        request.ApprovedStartTime = dto.ApprovedStartTime;
        request.ApprovedEndTime = dto.ApprovedEndTime;
        request.ApprovedRemarks = dto.ApprovedRemarks?.Trim();
        request.ReviewedByUserId = userId;
        request.ReviewedDate = DateTime.UtcNow;
        request.ReviewReason = dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
        request.RoutineScheduleId = save.Schedule!.RoutineScheduleId;
        request.UpdatedByUserId = userId;
        request.UpdatedDate = DateTime.UtcNow;
        AddAudit(request.RoutineScheduleRequestId, "Approve", previous, JsonSerializer.Serialize(new
        {
            Status = request.Status.ToString(), request.ApprovedDate, request.ApprovedStartTime,
            request.ApprovedEndTime, request.ApprovedRemarks, request.RoutineScheduleId,
        }), userId);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    public async Task<RoutineScheduleRequestBatchActionResult> ApproveBatchAsync(
        RoutineScheduleRequestBatchApproveDto dto, int userId)
    {
        var requestIds = dto.RequestIds.Distinct().ToList();
        var requests = await Query()
            .Where(x => requestIds.Contains(x.RoutineScheduleRequestId))
            .OrderBy(x => x.ProposedDate)
            .ThenBy(x => x.ProposedStartTime)
            .ThenBy(x => x.RoutineScheduleRequestId)
            .ToListAsync();

        if (requests.Count != requestIds.Count)
        {
            return new RoutineScheduleRequestBatchActionResult
            {
                NotFound = true,
                Error = "ไม่พบคำขอตารางฝึกซ้อมบางรายการ กรุณาโหลดข้อมูลใหม่",
            };
        }

        if (requests.Any(x => x.Status != RoutineScheduleRequestStatus.Pending))
        {
            return new RoutineScheduleRequestBatchActionResult
            {
                Error = "อนุมัติแบบชุดได้เฉพาะคำขอที่กำลังรอตรวจสอบ",
            };
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        foreach (var request in requests)
        {
            var previous = ProposalJson(request);
            var save = await _routineScheduleService.CreateAsync(new RoutineScheduleCreateDto
            {
                CoachId = request.CoachId,
                EffectiveStartDate = request.ProposedDate,
                StartTime = request.ProposedStartTime,
                EndTime = request.ProposedEndTime,
                Remarks = request.ProposedRemarks,
            }, userId);

            if (save.Error is not null)
            {
                await transaction.RollbackAsync();
                var coachName = request.Coach.Nickname ?? request.Coach.FullName;
                return new RoutineScheduleRequestBatchActionResult
                {
                    Error = $"ไม่สามารถอนุมัติคำขอของโค้ช {coachName} วันที่ {request.ProposedDate:dd/MM/yyyy}: {save.Error}",
                    Conflicts = save.Conflicts.Select(x => x.Message).ToList(),
                };
            }

            request.Status = RoutineScheduleRequestStatus.Approved;
            request.ApprovedDate = request.ProposedDate;
            request.ApprovedStartTime = request.ProposedStartTime;
            request.ApprovedEndTime = request.ProposedEndTime;
            request.ApprovedRemarks = request.ProposedRemarks?.Trim();
            request.ReviewedByUserId = userId;
            request.ReviewedDate = DateTime.UtcNow;
            request.ReviewReason = null;
            request.RoutineScheduleId = save.Schedule!.RoutineScheduleId;
            request.UpdatedByUserId = userId;
            request.UpdatedDate = DateTime.UtcNow;
            AddAudit(request.RoutineScheduleRequestId, "BatchApprove", previous, JsonSerializer.Serialize(new
            {
                Status = request.Status.ToString(), request.ApprovedDate, request.ApprovedStartTime,
                request.ApprovedEndTime, request.ApprovedRemarks, request.RoutineScheduleId,
            }), userId);
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new RoutineScheduleRequestBatchActionResult { Requests = requests.Select(Map).ToList() };
    }

    public Task<RoutineScheduleRequestActionResult> RequestRevisionAsync(int id, RoutineScheduleRequestDecisionDto dto, int userId) =>
        DecideAsync(id, RoutineScheduleRequestStatus.RevisionRequested, dto.Reason, userId);

    public Task<RoutineScheduleRequestActionResult> RejectAsync(int id, RoutineScheduleRequestDecisionDto dto, int userId) =>
        DecideAsync(id, RoutineScheduleRequestStatus.Rejected, dto.Reason, userId);

    public async Task<RoutineScheduleRequestActionResult> DeleteApprovedAsync(int id, int userId)
    {
        var request = await Query().FirstOrDefaultAsync(x => x.RoutineScheduleRequestId == id);
        if (request is null) return new RoutineScheduleRequestActionResult { NotFound = true };
        if (request.Status != RoutineScheduleRequestStatus.Approved || request.RoutineScheduleId is null)
            return new RoutineScheduleRequestActionResult { Error = "ลบได้เฉพาะคำขอที่อนุมัติและมีตารางฝึกซ้อมอยู่" };

        var previous = JsonSerializer.Serialize(new
        {
            Status = request.Status.ToString(),
            request.RoutineScheduleId,
            request.ApprovedDate,
            request.ApprovedStartTime,
            request.ApprovedEndTime,
        });
        var (found, error) = await _routineScheduleService.DeleteAsync(request.RoutineScheduleId.Value, userId);
        if (!found) return new RoutineScheduleRequestActionResult { Error = "ไม่พบตารางฝึกซ้อมที่อนุมัติไว้" };
        if (error is not null) return new RoutineScheduleRequestActionResult { Error = error };

        request.Status = RoutineScheduleRequestStatus.Cancelled;
        request.ReviewReason = "ผู้ดูแลลบตารางฝึกซ้อมที่อนุมัติแล้ว";
        request.ReviewedByUserId = userId;
        request.ReviewedDate = DateTime.UtcNow;
        request.UpdatedByUserId = userId;
        request.UpdatedDate = DateTime.UtcNow;
        AddAudit(request.RoutineScheduleRequestId, "DeleteApprovedSchedule", previous,
            JsonSerializer.Serialize(new { Status = request.Status.ToString(), request.ReviewReason }), userId);
        await _db.SaveChangesAsync();

        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    private async Task<RoutineScheduleRequestActionResult> DecideAsync(int id, RoutineScheduleRequestStatus status, string reason, int userId)
    {
        var request = await Query().FirstOrDefaultAsync(x => x.RoutineScheduleRequestId == id);
        if (request is null) return new RoutineScheduleRequestActionResult { NotFound = true };
        if (request.Status != RoutineScheduleRequestStatus.Pending)
            return new RoutineScheduleRequestActionResult { Error = "ดำเนินการได้เฉพาะคำขอที่กำลังรอตรวจสอบ" };
        if (string.IsNullOrWhiteSpace(reason)) return new RoutineScheduleRequestActionResult { Error = "กรุณาระบุเหตุผล" };

        var previous = ProposalJson(request);
        request.Status = status;
        request.ReviewReason = reason.Trim();
        request.ReviewedByUserId = userId;
        request.ReviewedDate = DateTime.UtcNow;
        request.UpdatedByUserId = userId;
        request.UpdatedDate = DateTime.UtcNow;
        AddAudit(request.RoutineScheduleRequestId, status == RoutineScheduleRequestStatus.Rejected ? "Reject" : "RequestRevision",
            previous, JsonSerializer.Serialize(new { Status = status.ToString(), Reason = request.ReviewReason }), userId);
        await _db.SaveChangesAsync();
        return new RoutineScheduleRequestActionResult { Request = Map(request) };
    }

    private async Task<RoutineScheduleRequestActionResult> ValidateProposalAsync(int coachId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeRequestId = null)
    {
        var coach = await _db.Coaches.FirstOrDefaultAsync(x => x.CoachId == coachId);
        if (coach is null || !coach.IsActive) return new RoutineScheduleRequestActionResult { Error = "ไม่พบข้อมูลโค้ชที่เปิดใช้งาน" };
        var conflicts = (await _conflictService.CheckRoutineTemplateOverlapAsync(coachId, start, end, date)).Select(x => x.Message).ToList();
        var pendingOverlap = await _db.RoutineScheduleRequests.AnyAsync(x => x.CoachId == coachId
            && x.Status == RoutineScheduleRequestStatus.Pending && x.ProposedDate == date
            && x.RoutineScheduleRequestId != excludeRequestId
            && x.ProposedStartTime < end && start < x.ProposedEndTime);
        if (pendingOverlap) conflicts.Add($"มีคำขอตารางสอนที่รออนุมัติทับซ้อนในวันที่ {date:dd/MM/yyyy}");
        return conflicts.Count == 0
            ? new RoutineScheduleRequestActionResult()
            : new RoutineScheduleRequestActionResult { Error = "พบตารางหรือคำขอของโค้ชทับซ้อนกัน", Conflicts = conflicts };
    }

    private IQueryable<RoutineScheduleRequest> Query() => _db.RoutineScheduleRequests
        .Include(x => x.Coach)
        .Include(x => x.RoutineSchedule)
        .ThenInclude(x => x!.Coach);

    private void AddAudit(int id, string action, string? previous, string? next, int userId) => _db.AuditLogs.Add(new AuditLog
    {
        EntityName = nameof(RoutineScheduleRequest), EntityId = id, Action = action,
        PreviousValue = previous, NewValue = next, ActionByUserId = userId,
    });

    private static string ProposalJson(RoutineScheduleRequest request) => JsonSerializer.Serialize(new
    {
        Status = request.Status.ToString(), request.ProposedDate, request.ProposedStartTime,
        request.ProposedEndTime, request.ProposedRemarks,
    });

    private static RoutineScheduleRequestDto Map(RoutineScheduleRequest x) => new()
    {
        RoutineScheduleRequestId = x.RoutineScheduleRequestId, SubmissionBatchId = x.SubmissionBatchId,
        CoachId = x.CoachId, CoachCode = x.Coach.CoachCode, CoachName = x.Coach.FullName,
        CoachNickname = x.Coach.Nickname, CoachColorHex = x.Coach.ColorHex,
        ProposedDate = x.ProposedDate, ProposedStartTime = x.ProposedStartTime, ProposedEndTime = x.ProposedEndTime,
        ProposedRemarks = x.ProposedRemarks, Status = x.Status, ApprovedDate = x.ApprovedDate,
        ApprovedStartTime = x.ApprovedStartTime, ApprovedEndTime = x.ApprovedEndTime, ApprovedRemarks = x.ApprovedRemarks,
        ReviewReason = x.ReviewReason, ReviewedDate = x.ReviewedDate, RoutineScheduleId = x.RoutineScheduleId, CreatedDate = x.CreatedDate,
        CurrentScheduleCoachId = x.RoutineSchedule?.CoachId,
        CurrentScheduleCoachCode = x.RoutineSchedule?.Coach.CoachCode,
        CurrentScheduleCoachName = x.RoutineSchedule?.Coach.FullName,
        CurrentScheduleCoachNickname = x.RoutineSchedule?.Coach.Nickname,
        CurrentScheduleCoachColorHex = x.RoutineSchedule?.Coach.ColorHex,
        CurrentScheduleDate = x.RoutineSchedule?.EffectiveStartDate,
        CurrentScheduleStartTime = x.RoutineSchedule?.StartTime,
        CurrentScheduleEndTime = x.RoutineSchedule?.EndTime,
        CurrentScheduleRemarks = x.RoutineSchedule?.Remarks,
    };

}
