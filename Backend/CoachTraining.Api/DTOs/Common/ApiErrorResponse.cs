namespace CoachTraining.Api.DTOs.Common;

/// <summary>
/// Standard error response envelope. See skill.md "API Response Standards" and
/// "Validation Error Response Example".
/// </summary>
public class ApiErrorResponse
{
    public string Message { get; set; } = "Error";

    public List<ApiFieldError> Errors { get; set; } = [];

    public ApiErrorResponse()
    {
    }

    public ApiErrorResponse(string message, List<ApiFieldError>? errors = null)
    {
        Message = message;
        Errors = errors ?? [];
    }
}

public class ApiFieldError
{
    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public ApiFieldError()
    {
    }

    public ApiFieldError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
