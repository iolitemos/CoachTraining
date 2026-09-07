namespace CoachTraining.Api.Models.Common;

/// <summary>
/// Common audit columns required by skill.md "Common Columns" for every
/// important business table. Each entity still declares its own explicit
/// primary key (e.g. CoachId) rather than inheriting a generic Id.
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
}
