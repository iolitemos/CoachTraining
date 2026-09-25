using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>An athlete's explicit plan to attend Routine Training on one date; never an Attendance record.</summary>
public class RoutineParticipationPlan : AuditableEntity
{
    public int RoutineParticipationPlanId { get; set; }
    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;
    public DateOnly TrainingDate { get; set; }
}
