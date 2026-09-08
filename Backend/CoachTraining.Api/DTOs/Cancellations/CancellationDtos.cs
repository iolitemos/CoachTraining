using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.TrainingSessions;

namespace CoachTraining.Api.DTOs.Cancellations;

/// <summary>Cancellation reason required by FR-CR-002.</summary>
public class CancelSessionRequest
{
    [Required(ErrorMessage = "กรุณาระบุเหตุผลในการยกเลิก")]
    [MaxLength(1000, ErrorMessage = "เหตุผลต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Service-layer result translated to HTTP semantics by the controller.</summary>
public class CancellationActionResult
{
    public TrainingSessionDetailDto? Session { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
}
