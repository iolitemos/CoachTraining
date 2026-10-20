using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Compensations;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Administrator-only monthly compensation calculation and finalization.</summary>
public class CoachCompensationService : ICoachCompensationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CoachCompensationService> _logger;

    public CoachCompensationService(ApplicationDbContext db, ILogger<CoachCompensationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CoachCompensationStatementDto?> GetAsync(int coachId, int year, int month)
    {
        var statement = await StatementQuery()
            .FirstOrDefaultAsync(item => item.CoachId == coachId && item.Year == year && item.Month == month);
        return statement is null ? null : Map(statement);
    }

    public async Task<CoachCompensationActionResult> CalculateAsync(CoachCompensationPeriodRequest request, int actionByUserId)
    {
        try
        {
            var coach = await _db.Coaches.FirstOrDefaultAsync(item => item.CoachId == request.CoachId);
            if (coach is null)
            {
                return new CoachCompensationActionResult { NotFound = true };
            }

            var statement = await _db.CoachCompensationStatements
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.CoachId == request.CoachId && item.Year == request.Year && item.Month == request.Month);

            if (statement is not null && statement.Status != CompensationStatementStatus.Draft)
            {
                return new CoachCompensationActionResult { Error = "ไม่สามารถคำนวณรายการที่ยืนยันหรือจ่ายแล้วซ้ำได้" };
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            var startDate = new DateOnly(request.Year, request.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var sessions = await _db.TrainingSessions
                .Include(item => item.PrivateAthletes)
                .Where(item => item.TrainingType == TrainingType.Private
                    && item.Status == SessionStatus.Locked
                    && item.SessionDate >= startDate
                    && item.SessionDate <= endDate
                    && (item.ActualCoachId ?? item.AssignedCoachId) == request.CoachId)
                .OrderBy(item => item.SessionDate)
                .ThenBy(item => item.ScheduledStartDateTime)
                .ToListAsync();

            var actionDate = DateTime.UtcNow;
            var reusableItems = statement?.Items.ToDictionary(item => item.TrainingSessionId)
                ?? new Dictionary<int, CoachCompensationItem>();
            if (statement is null)
            {
                statement = new CoachCompensationStatement
                {
                    CoachId = coach.CoachId,
                    Year = request.Year,
                    Month = request.Month,
                    CreatedByUserId = actionByUserId,
                };
                _db.CoachCompensationStatements.Add(statement);
            }

            statement.BaseSalarySnapshot = coach.BaseSalary ?? 0m;
            statement.UpdatedByUserId = actionByUserId;
            statement.UpdatedDate = actionDate;

            foreach (var session in sessions)
            {
                session.PrivateRateSnapshot ??= coach.PrivateRate ?? 0m;
                var participants = string.Join(", ", session.PrivateAthletes
                    .OrderBy(item => item.AthleteNameSnapshot)
                    .Select(item => string.IsNullOrWhiteSpace(item.AthleteNicknameSnapshot)
                        ? item.AthleteNameSnapshot
                        : item.AthleteNicknameSnapshot));

                if (!reusableItems.Remove(session.TrainingSessionId, out var compensationItem))
                {
                    compensationItem = new CoachCompensationItem
                    {
                        TrainingSessionId = session.TrainingSessionId,
                        CreatedByUserId = actionByUserId,
                    };
                    statement.Items.Add(compensationItem);
                }

                compensationItem.SessionDateSnapshot = session.SessionDate;
                compensationItem.ScheduledStartSnapshot = session.ScheduledStartDateTime;
                compensationItem.ScheduledEndSnapshot = session.ScheduledEndDateTime;
                compensationItem.CoachNameSnapshot = session.ActualCoachNameSnapshot ?? session.AssignedCoachNameSnapshot;
                compensationItem.ParticipantNamesSnapshot = participants;
                compensationItem.PrivateRateSnapshot = session.PrivateRateSnapshot.Value;
                compensationItem.Amount = session.PrivateRateSnapshot.Value;
                compensationItem.UpdatedByUserId = actionByUserId;
                compensationItem.UpdatedDate = actionDate;
            }

            // Draft rows that are no longer eligible (for example, an unlocked
            // session) are disposable working data. Confirmed/Paid statements never
            // reach this branch and therefore retain their immutable item snapshots.
            var obsoleteItems = reusableItems.Values.ToList();
            _db.CoachCompensationItems.RemoveRange(obsoleteItems);
            foreach (var obsoleteItem in obsoleteItems)
            {
                statement.Items.Remove(obsoleteItem);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await LoadByIdAsync(statement.CoachCompensationStatementId);
            return new CoachCompensationActionResult { Data = Map(result!) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate Coach compensation. Service: CoachCompensationService Function: CalculateAsync CoachId: {CoachId} Year: {Year} Month: {Month} ActionByUserId: {ActionByUserId}", request.CoachId, request.Year, request.Month, actionByUserId);
            throw;
        }
    }

    public async Task<CoachCompensationActionResult> UpdateAdjustmentAsync(int statementId, CoachCompensationAdjustmentRequest request, int actionByUserId)
    {
        var statement = await LoadByIdAsync(statementId);
        if (statement is null)
        {
            return new CoachCompensationActionResult { NotFound = true };
        }

        if (statement.Status != CompensationStatementStatus.Draft)
        {
            return new CoachCompensationActionResult { Error = "แก้ไขรายการปรับเพิ่ม/ลดได้เฉพาะรายการฉบับร่าง" };
        }

        var reason = request.Reason?.Trim();
        if (request.Amount != 0 && string.IsNullOrWhiteSpace(reason))
        {
            return new CoachCompensationActionResult { Error = "กรุณาระบุเหตุผลของรายการปรับเพิ่ม/ลด" };
        }

        statement.AdjustmentAmount = request.Amount;
        statement.AdjustmentReason = request.Amount == 0 ? null : reason;
        statement.UpdatedByUserId = actionByUserId;
        statement.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CoachCompensationActionResult { Data = Map(statement) };
    }

    public Task<CoachCompensationActionResult> ConfirmAsync(int statementId, int actionByUserId) =>
        ChangeStatusAsync(statementId, CompensationStatementStatus.Draft, CompensationStatementStatus.Confirmed, actionByUserId);

    public Task<CoachCompensationActionResult> MarkPaidAsync(int statementId, int actionByUserId) =>
        ChangeStatusAsync(statementId, CompensationStatementStatus.Confirmed, CompensationStatementStatus.Paid, actionByUserId);

    private async Task<CoachCompensationActionResult> ChangeStatusAsync(
        int statementId,
        CompensationStatementStatus requiredStatus,
        CompensationStatementStatus nextStatus,
        int actionByUserId)
    {
        var statement = await LoadByIdAsync(statementId);
        if (statement is null)
        {
            return new CoachCompensationActionResult { NotFound = true };
        }

        if (statement.Status != requiredStatus)
        {
            return new CoachCompensationActionResult { Error = $"ไม่สามารถเปลี่ยนสถานะจาก {statement.Status} เป็น {nextStatus} ได้" };
        }

        var actionDate = DateTime.UtcNow;
        statement.Status = nextStatus;
        statement.UpdatedByUserId = actionByUserId;
        statement.UpdatedDate = actionDate;
        if (nextStatus == CompensationStatementStatus.Confirmed)
        {
            statement.ConfirmedByUserId = actionByUserId;
            statement.ConfirmedDate = actionDate;
        }
        else
        {
            statement.PaidByUserId = actionByUserId;
            statement.PaidDate = actionDate;
        }

        await _db.SaveChangesAsync();
        return new CoachCompensationActionResult { Data = Map(statement) };
    }

    private IQueryable<CoachCompensationStatement> StatementQuery() =>
        _db.CoachCompensationStatements
            .Include(statement => statement.Coach)
            .Include(statement => statement.Items)
            .AsSplitQuery();

    private Task<CoachCompensationStatement?> LoadByIdAsync(int statementId) =>
        StatementQuery().FirstOrDefaultAsync(statement => statement.CoachCompensationStatementId == statementId);

    private static CoachCompensationStatementDto Map(CoachCompensationStatement statement)
    {
        var items = statement.Items
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.SessionDateSnapshot)
            .ThenBy(item => item.ScheduledStartSnapshot)
            .Select(item => new CoachCompensationItemDto
            {
                CoachCompensationItemId = item.CoachCompensationItemId,
                TrainingSessionId = item.TrainingSessionId,
                SessionDate = item.SessionDateSnapshot,
                ScheduledStartDateTime = item.ScheduledStartSnapshot,
                ScheduledEndDateTime = item.ScheduledEndSnapshot,
                CoachName = item.CoachNameSnapshot,
                ParticipantNames = item.ParticipantNamesSnapshot,
                PrivateRate = item.PrivateRateSnapshot,
                Amount = item.Amount,
            })
            .ToList();
        var privateTotal = items.Sum(item => item.Amount);

        return new CoachCompensationStatementDto
        {
            CoachCompensationStatementId = statement.CoachCompensationStatementId,
            CoachId = statement.CoachId,
            CoachCode = statement.Coach.CoachCode,
            CoachFullName = statement.Coach.FullName,
            CoachNickname = statement.Coach.Nickname,
            Year = statement.Year,
            Month = statement.Month,
            Status = statement.Status.ToString(),
            BaseSalary = statement.BaseSalarySnapshot,
            PrivateSessionCount = items.Count,
            PrivateCompensation = privateTotal,
            AdjustmentAmount = statement.AdjustmentAmount,
            AdjustmentReason = statement.AdjustmentReason,
            NetAmount = statement.BaseSalarySnapshot + privateTotal + statement.AdjustmentAmount,
            ConfirmedDate = statement.ConfirmedDate,
            PaidDate = statement.PaidDate,
            Items = items,
        };
    }
}
