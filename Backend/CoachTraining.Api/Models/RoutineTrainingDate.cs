using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>A date on which athletes may plan to attend Routine Training, independent of Coach schedules.</summary>
public class RoutineTrainingDate : AuditableEntity
{
    public int RoutineTrainingDateId { get; set; }
    public DateOnly TrainingDate { get; set; }
}
