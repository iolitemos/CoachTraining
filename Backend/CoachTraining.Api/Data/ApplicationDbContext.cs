using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Data;

/// <summary>
/// Primary EF Core database context for the Coach Training & Athlete Attendance
/// Management System. See todo.md section 2 for the entity-by-entity design
/// notes and migration grouping.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // 2.1 Identity and Access Data
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // 2.2 / 2.3 Coach and Athlete Data
    public DbSet<Coach> Coaches => Set<Coach>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<CompetitionMatch> CompetitionMatches => Set<CompetitionMatch>();
    public DbSet<RoutineCalendarShareLink> RoutineCalendarShareLinks => Set<RoutineCalendarShareLink>();
    public DbSet<CalendarNote> CalendarNotes => Set<CalendarNote>();

    // 2.4 Routine Schedule Data
    public DbSet<RoutineSchedule> RoutineSchedules => Set<RoutineSchedule>();

    // 2.5 / 2.6 Training Session and Private Athlete Assignment Data
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<PrivateSessionAthlete> PrivateSessionAthletes => Set<PrivateSessionAthlete>();

    // 2.7 / 2.8 Attendance and Training Log Data
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<TrainingLog> TrainingLogs => Set<TrainingLog>();

    // 2.9 / 2.10 / 2.11 Substitution, Approval, Conflict-Override, and Audit Data
    public DbSet<CoachSubstitutionHistory> CoachSubstitutionHistories => Set<CoachSubstitutionHistory>();
    public DbSet<TrainingApprovalHistory> TrainingApprovalHistories => Set<TrainingApprovalHistory>();
    public DbSet<ConflictOverrideHistory> ConflictOverrideHistories => Set<ConflictOverrideHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
