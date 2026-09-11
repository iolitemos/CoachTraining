namespace CoachTraining.Api.DTOs.Athletes;

public class AthleteImportResultDto
{
    public int ImportedCount { get; set; }
    public int TotalRows { get; set; }
}

public class AthleteImportValidationErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AthleteImportValidationException : Exception
{
    public AthleteImportValidationException(IEnumerable<AthleteImportValidationErrorDto> errors)
        : base("ข้อมูลในไฟล์นำเข้าไม่ถูกต้อง")
    {
        Errors = errors.ToList();
    }

    public IReadOnlyList<AthleteImportValidationErrorDto> Errors { get; }
}
