using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.CompetitionMatches;

public class CompetitionMatchRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณากรอกชื่อรายการ")]
    [MaxLength(200, ErrorMessage = "ชื่อรายการต้องไม่เกิน 200 ตัวอักษร")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกจังหวัด")]
    [MaxLength(100, ErrorMessage = "จังหวัดต้องไม่เกิน 100 ตัวอักษร")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวันที่เริ่มแข่ง")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันที่สิ้นสุดการแข่งขัน")]
    public DateOnly? EndDate { get; set; }

    [MinLength(1, ErrorMessage = "กรุณาเลือกโค้ชอย่างน้อย 1 คน")]
    public List<int> CoachIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("กรุณากรอกชื่อรายการ", [nameof(Name)]);
        }

        if (string.IsNullOrWhiteSpace(Province))
        {
            yield return new ValidationResult("กรุณากรอกจังหวัด", [nameof(Province)]);
        }

        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
        {
            yield return new ValidationResult(
                "วันที่สิ้นสุดการแข่งขันต้องไม่ก่อนวันที่เริ่มแข่ง",
                [nameof(EndDate)]);
        }

        if (CoachIds.Count == 0)
        {
            yield return new ValidationResult("กรุณาเลือกโค้ชอย่างน้อย 1 คน", [nameof(CoachIds)]);
        }

        if (CoachIds.Count != CoachIds.Distinct().Count())
        {
            yield return new ValidationResult("ไม่สามารถเลือกโค้ชซ้ำในรายการเดียวกันได้", [nameof(CoachIds)]);
        }
    }
}

public class CompetitionMatchDto
{
    public int CompetitionMatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<CompetitionMatchCoachDto> Coaches { get; set; } = [];
}

public record CompetitionMatchCoachDto(int CoachId, string FullName, string? Nickname);
