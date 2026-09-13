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
            .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
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
                a.PrivateSessionAthleteId,
                ParticipantIdentity = a.AthleteId ?? -a.PrivateSessionAthleteId!.Value,
                a.AthleteCodeSnapshot,
                a.AthleteNameSnapshot,
                Nickname = a.AthleteId.HasValue ? a.Athlete!.Nickname : null,
                GuestPhone = a.PrivateSessionAthlete != null ? a.PrivateSessionAthlete.GuestPhone : null,
                a.TrainingSessionId,
                a.TrainingSession.TrainingType,
                a.TrainingSession.SessionDate,
                a.Status,
                a.ArrivalTime,
                a.Remark,
            })
            .ToListAsync();

        var items = records
            .GroupBy(r => new { r.ParticipantIdentity, r.AthleteId, r.AthleteCodeSnapshot, r.AthleteNameSnapshot, r.Nickname, r.GuestPhone })
            .Select(g => new AthleteAttendanceReportItemDto
            {
                AthleteId = g.Key.AthleteId,
                IsGuest = g.Key.AthleteId is null,
                ParticipantKey = g.Key.AthleteId is not null ? $"athlete-{g.Key.AthleteId}" : $"guest-{-g.Key.ParticipantIdentity}",
                GuestPhone = g.Key.GuestPhone,
                AthleteCode = g.Key.AthleteCodeSnapshot,
                FullName = g.Key.AthleteNameSnapshot,
                Nickname = g.Key.Nickname,
                RoutineAttendanceCount = g.Count(r => r.TrainingType == TrainingType.Routine),
                PrivateAttendanceCount = g.Count(r => r.TrainingType == TrainingType.Private),
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
            .OrderBy(i => i.Nickname ?? i.FullName)
            .ToList();

        return new AthleteAttendanceReportResponseDto { Items = items };
    }
}
