using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.TrainingSessions;

public class ResetSessionRequest
{
    [Required, MinLength(1)]
    public string Reason { get; set; } = string.Empty;
}
