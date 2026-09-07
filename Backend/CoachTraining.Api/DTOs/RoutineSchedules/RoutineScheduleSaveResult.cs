using CoachTraining.Api.DTOs.Conflicts;

namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class RoutineScheduleSaveResult
{
    public RoutineScheduleDetailDto? Schedule { get; set; }
    public string? Error { get; set; }
    public List<RoutineTemplateConflictDetail> Conflicts { get; set; } = [];

    /// <summary>Populated only on create — the sessions generated for the initial window.</summary>
    public GenerateSessionsResult? InitialGeneration { get; set; }
}
