using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Athlete Attendance Report (requirement.md 6.19, todo.md 4.19).</summary>
public class AthleteAttendanceReportService : IAthleteAttendanceReportService
{
    public AthleteAttendanceReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    private readonly ApplicationDbContext _db;

    public async Task<AthleteAttendanceReportResponseDto> GetReportAsync(AthleteAttendanceReportFilter filter)
    {
        var query = _db.Attendances
            // FR-RPT-ATH-007 — a Rescheduled-original session never happened at its
            // original time; its attendance (if any was recorded before the move)
            // must not double-count against the replacement session.
            .Where(a => a.TrainingSession.Status != SessionStatus.Rescheduled)
            .AsQueryable();

        if (filter.AthleteId is not null)
        {
            query = query.Where(a => a.AthleteId == filter.AthleteId.Value);
        }

        if (filter.StartDate is not null)
        {
            query = query.Where(a => a.TrainingSession.SessionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate is not null)
        {
            query = query.Where(a => a.TrainingSession.SessionDate <= filter.EndDate.Value);
        }

        var records = await query
            .Select(a => new
            {
                a.AthleteId,
                a.AthleteCodeSnapshot,
                a.AthleteNameSnapshot,
                a.TrainingSessionId,
                a.TrainingSession.TrainingType,
                a.TrainingSession.SessionDate,
                a.Status,
                a.ArrivalTime,
                a.Remark,
            })
            .ToListAsync();

        var items = records
            .GroupBy(r => new { r.AthleteId, r.AthleteCodeSnapshot, r.AthleteNameSnapshot })
            .Select(g => new AthleteAttendanceReportItemDto
            {
                AthleteId = g.Key.AthleteId,
                AthleteCode = g.Key.AthleteCodeSnapshot,
                FullName = g.Key.AthleteNameSnapshot,
                RoutinePresentCount = g.Count(r => r.TrainingType == TrainingType.Routine && r.Status == AttendanceStatus.Present),
                RoutineLateCount = g.Count(r => r.TrainingType == TrainingType.Routine && r.Status == AttendanceStatus.Late),
                PrivatePresentCount = g.Count(r => r.TrainingType == TrainingType.Private && r.Status == AttendanceStatus.Present),
                PrivateAbsentCount = g.Count(r => r.TrainingType == TrainingType.Private && r.Status == AttendanceStatus.Absent),
                PrivateLateCount = g.Count(r => r.TrainingType == TrainingType.Private && r.Status == AttendanceStatus.Late),
                PrivateExcusedCount = g.Count(r => r.TrainingType == TrainingType.Private && r.Status == AttendanceStatus.Excused),
                Records = g
                    .OrderByDescending(r => r.SessionDate)
                    .Select(r => new AthleteAttendanceRecordDto
                    {
                        TrainingSessionId = r.TrainingSessionId,
                        TrainingType = r.TrainingType,
                        SessionDate = r.SessionDate,
                        Status = r.Status,
                        ArrivalTime = r.ArrivalTime,
                        Remark = r.Remark,
                    })
                    .ToList(),
            })
            .OrderBy(i => i.FullName)
            .ToList();

        return new AthleteAttendanceReportResponseDto { Items = items };
    }
}
