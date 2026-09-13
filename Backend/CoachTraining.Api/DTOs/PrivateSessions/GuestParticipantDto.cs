using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class GuestParticipantDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้เรียนชั่วคราว")]
    [MaxLength(200, ErrorMessage = "ชื่อต้องมีความยาวไม่เกิน 200 ตัวอักษร")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "เบอร์โทรศัพท์ต้องมีความยาวไม่เกิน 50 ตัวอักษร")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "หมายเหตุต้องมีความยาวไม่เกิน 500 ตัวอักษร")]
    public string? Remark { get; set; }
}
