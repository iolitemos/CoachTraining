using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Users;

public class UserUpdateDto
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;
}

public class UserStatusUpdateDto
{
    public bool IsActive { get; set; }
}

public class AssignRolesDto
{
    [Required(ErrorMessage = "กรุณาเลือกอย่างน้อยหนึ่งบทบาท")]
    [MinLength(1, ErrorMessage = "กรุณาเลือกอย่างน้อยหนึ่งบทบาท")]
    public List<int> RoleIds { get; set; } = [];
}

public class CoachLinkDto
{
    /// <summary>Null unlinks the coach from this user account.</summary>
    public int? CoachId { get; set; }
}

public class RoleOptionDto
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
}
