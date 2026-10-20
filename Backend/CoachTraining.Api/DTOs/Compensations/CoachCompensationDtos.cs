using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Compensations;

public class CoachCompensationPeriodRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกโค้ช")]
    public int CoachId { get; set; }

    [Range(2000, 2100, ErrorMessage = "ปีไม่ถูกต้อง")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "เดือนไม่ถูกต้อง")]
    public int Month { get; set; }
}

public class CoachCompensationAdjustmentRequest
{
    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99", ErrorMessage = "จำนวนเงินปรับเพิ่ม/ลดเกินขอบเขตที่รองรับ")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class CoachCompensationItemDto
{
    public int CoachCompensationItemId { get; set; }
    public int TrainingSessionId { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }
    public string CoachName { get; set; } = string.Empty;
    public string ParticipantNames { get; set; } = string.Empty;
    public decimal PrivateRate { get; set; }
    public decimal Amount { get; set; }
}

public class CoachCompensationStatementDto
{
    public int CoachCompensationStatementId { get; set; }
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public string? CoachNickname { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public int PrivateSessionCount { get; set; }
    public decimal PrivateCompensation { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string? AdjustmentReason { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public IReadOnlyList<CoachCompensationItemDto> Items { get; set; } = [];
}

public class CoachCompensationActionResult
{
    public CoachCompensationStatementDto? Data { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
}
