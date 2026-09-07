namespace CoachTraining.Api.DTOs.Common;

/// <summary>
/// Standard success response envelope. See skill.md "API Response Standards".
/// </summary>
public class ApiResponse<T>
{
    public string Message { get; set; } = "Success";

    public T? Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(T? data, string message = "Success")
    {
        Data = data;
        Message = message;
    }

    public static ApiResponse<T> Success(T? data, string message = "Success") => new(data, message);
}
