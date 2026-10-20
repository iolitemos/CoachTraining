using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.RoutineScheduleRequests;

public class RoutineScheduleRequestCreateDto : IValidatableObject
{
    public DateOnly ProposedDate { get; set; }
    public TimeOnly ProposedStartTime { get; set; }
    public TimeOnly ProposedEndTime { get; set; }
    [MaxLength(1000)] public string? ProposedRemarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProposedEndTime <= ProposedStartTime)
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(ProposedEndTime)]);
    }
}

public class RoutineScheduleRequestBatchCreateDto : IValidatableObject
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    [MaxLength(1000)] public string? Remarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime) yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        if (EndDate < StartDate) yield return new ValidationResult("วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่ม", [nameof(EndDate)]);
        if (EndDate.DayNumber - StartDate.DayNumber > 366) yield return new ValidationResult("ช่วงวันที่ต้องไม่เกิน 366 วัน", [nameof(EndDate)]);
        if (DaysOfWeek.Count == 0) yield return new ValidationResult("กรุณาเลือกวันในสัปดาห์อย่างน้อย 1 วัน", [nameof(DaysOfWeek)]);
    }
}

public class RoutineScheduleRequestReviewDto : IValidatableObject
{
    public DateOnly ApprovedDate { get; set; }
    public TimeOnly ApprovedStartTime { get; set; }
    public TimeOnly ApprovedEndTime { get; set; }
    [MaxLength(1000)] public string? ApprovedRemarks { get; set; }
    public bool OverrideConflict { get; set; }
    [MaxLength(1000)] public string? OverrideReason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ApprovedEndTime <= ApprovedStartTime) yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(ApprovedEndTime)]);
        if (OverrideConflict && string.IsNullOrWhiteSpace(OverrideReason)) yield return new ValidationResult("กรุณาระบุเหตุผลการยืนยันตารางทับซ้อน", [nameof(OverrideReason)]);
    }
}

public class RoutineScheduleRequestDecisionDto
{
    [Required, MaxLength(1000)] public string Reason { get; set; } = string.Empty;
}

public class RoutineScheduleRequestBatchApproveDto : IValidatableObject
{
    [Required, MinLength(1), MaxLength(200)]
    public List<int> RequestIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequestIds.Any(id => id <= 0))
            yield return new ValidationResult("รหัสคำขอต้องถูกต้อง", [nameof(RequestIds)]);
        if (RequestIds.Count != RequestIds.Distinct().Count())
            yield return new ValidationResult("ไม่สามารถเลือกคำขอซ้ำกันได้", [nameof(RequestIds)]);
    }
}

public class RoutineScheduleRequestDto
{
    public int RoutineScheduleRequestId { get; set; }
    public Guid SubmissionBatchId { get; set; }
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachName { get; set; } = string.Empty;
    public string? CoachNickname { get; set; }
    public string CoachColorHex { get; set; } = "#10B981";
    public DateOnly ProposedDate { get; set; }
    public TimeOnly ProposedStartTime { get; set; }
    public TimeOnly ProposedEndTime { get; set; }
    public string? ProposedRemarks { get; set; }
    public RoutineScheduleRequestStatus Status { get; set; }
    public DateOnly? ApprovedDate { get; set; }
    public TimeOnly? ApprovedStartTime { get; set; }
    public TimeOnly? ApprovedEndTime { get; set; }
    public string? ApprovedRemarks { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public int? RoutineScheduleId { get; set; }
    public int? CurrentScheduleCoachId { get; set; }
    public string? CurrentScheduleCoachCode { get; set; }
    public string? CurrentScheduleCoachName { get; set; }
    public string? CurrentScheduleCoachNickname { get; set; }
    public string? CurrentScheduleCoachColorHex { get; set; }
    public DateOnly? CurrentScheduleDate { get; set; }
    public TimeOnly? CurrentScheduleStartTime { get; set; }
    public TimeOnly? CurrentScheduleEndTime { get; set; }
    public string? CurrentScheduleRemarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class RoutineScheduleRequestActionResult
{
    public RoutineScheduleRequestDto? Request { get; set; }
    public string? Error { get; set; }
    public List<string> Conflicts { get; set; } = [];
    public bool NotFound { get; set; }
}

public class RoutineScheduleRequestBatchActionResult
{
    public List<RoutineScheduleRequestDto> Requests { get; set; } = [];
    public string? Error { get; set; }
    public List<string> Conflicts { get; set; } = [];
    public bool NotFound { get; set; }
}
