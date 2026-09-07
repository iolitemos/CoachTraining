using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.TrainingSessions;

/// <summary>
/// Filters for the unified Training Session query (todo.md 4.5) — by coach, training
/// type, status, and date range. A Coach account never sees another coach's sessions
/// regardless of the CoachId value supplied here (enforced in the service/controller).
/// </summary>
public class TrainingSessionFilterRequest : PagedRequest
{
    public int? CoachId { get; set; }
    public TrainingType? TrainingType { get; set; }
    public SessionStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
