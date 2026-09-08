using CoachTraining.Api.DTOs.PrivateSessions;
using CoachTraining.Api.DTOs.TrainingLogs;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Models;

namespace CoachTraining.Api.Helpers;

/// <summary>Shared TrainingSession -> DTO mapping, reused by every service that returns session detail.</summary>
public static class TrainingSessionMapper
{
    public static TrainingSessionDetailDto ToDetailDto(TrainingSession session) => new()
    {
        TrainingSessionId = session.TrainingSessionId,
        TrainingType = session.TrainingType,
        RoutineScheduleId = session.RoutineScheduleId,
        SessionDate = session.SessionDate,
        ScheduledStartDateTime = session.ScheduledStartDateTime,
        ScheduledEndDateTime = session.ScheduledEndDateTime,
        ActualStartDateTime = session.ActualStartDateTime,
        ActualEndDateTime = session.ActualEndDateTime,
        ActualDurationMinutes = session.ActualStartDateTime is not null && session.ActualEndDateTime is not null
            ? (int)(session.ActualEndDateTime.Value - session.ActualStartDateTime.Value).TotalMinutes
            : null,
        AssignedCoachId = session.AssignedCoachId,
        AssignedCoachCode = session.AssignedCoachCodeSnapshot,
        AssignedCoachName = session.AssignedCoachNameSnapshot,
        AssignedCoachNickname = session.AssignedCoach?.Nickname,
        AssignedCoachColorHex = session.AssignedCoach?.ColorHex ?? "#10B981",
        ActualCoachId = session.ActualCoachId,
        ActualCoachCode = session.ActualCoachCodeSnapshot,
        ActualCoachName = session.ActualCoachNameSnapshot,
        ActualCoachNickname = session.ActualCoach?.Nickname,
        ActualCoachColorHex = session.ActualCoach?.ColorHex,
        Status = session.Status,
        Location = session.Location,
        Remarks = session.Remarks,
        CancellationReason = session.CancellationReason,
        OriginalSessionId = session.OriginalSessionId,
        IsConflictOverridden = session.IsConflictOverridden,
        ConflictOverrideReason = session.ConflictOverrideReason,
        Athletes = session.PrivateAthletes.Select(psa => new PrivateSessionAthleteDto
        {
            AthleteId = psa.AthleteId,
            AthleteCode = psa.AthleteCodeSnapshot,
            FullName = psa.AthleteNameSnapshot,
        }).ToList(),
        TrainingLog = session.TrainingLog is null ? null : new TrainingLogDto
        {
            TrainingLogId = session.TrainingLog.TrainingLogId,
            Topic = session.TrainingLog.Topic,
            Objective = session.TrainingLog.Objective,
            ExerciseDrill = session.TrainingLog.ExerciseDrill,
            Focus = session.TrainingLog.Focus,
            Intensity = session.TrainingLog.Intensity,
            CoachNotes = session.TrainingLog.CoachNotes,
            AthleteNotes = session.TrainingLog.AthleteNotes,
            GeneralRemarks = session.TrainingLog.GeneralRemarks,
            UpdatedDate = session.TrainingLog.UpdatedDate,
        },
    };
}
