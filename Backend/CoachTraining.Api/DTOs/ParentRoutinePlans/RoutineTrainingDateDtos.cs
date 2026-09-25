using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.ParentRoutinePlans;

public record RoutineTrainingDateDto(int RoutineTrainingDateId, DateOnly TrainingDate);

public class RoutineTrainingDateCreateDto
{
    [Required]
    public DateOnly? TrainingDate { get; set; }
}
