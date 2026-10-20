using CoachTraining.Api.Models.Common;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>Monthly compensation statement visible only to Administrators.</summary>
public class CoachCompensationStatement : AuditableEntity
{
    public int CoachCompensationStatementId { get; set; }

    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal BaseSalarySnapshot { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string? AdjustmentReason { get; set; }

    public CompensationStatementStatus Status { get; set; } = CompensationStatementStatus.Draft;

    public DateTime? ConfirmedDate { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public DateTime? PaidDate { get; set; }
    public int? PaidByUserId { get; set; }

    public ICollection<CoachCompensationItem> Items { get; set; } = [];
}
