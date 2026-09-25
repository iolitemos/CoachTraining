using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoachTraining.Api.Tests.Services;

public class RoutineTrainingDateServiceTests
{
    [Fact]
    public async Task AddAndRemove_AreIndependentFromCoachRoutineSchedules()
    {
        using var db = TestDbContextFactory.Create();
        var coach = new Coach { CoachCode = "C001", FullName = "Coach", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        var scheduleDate = new DateOnly(2026, 10, 3);
        var availabilityDate = new DateOnly(2026, 10, 4);
        db.RoutineSchedules.Add(new RoutineSchedule
        {
            CoachId = coach.CoachId,
            EffectiveStartDate = scheduleDate,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            IsActive = true,
        });
        await db.SaveChangesAsync();
        var service = new RoutineTrainingDateService(db, NullLogger<RoutineTrainingDateService>.Instance);

        Assert.Empty(await service.ListAsync(scheduleDate, availabilityDate));
        await service.AddAsync(availabilityDate, 7);
        var item = Assert.Single(await service.ListAsync(scheduleDate, availabilityDate));
        Assert.Equal(availabilityDate, item.TrainingDate);
        Assert.Single(db.RoutineSchedules);

        Assert.True(await service.RemoveAsync(availabilityDate, 7));
        Assert.Empty(await service.ListAsync(scheduleDate, availabilityDate));
        Assert.Single(db.RoutineSchedules);
    }

    [Fact]
    public async Task AddAsync_ReactivatesPreviouslyRemovedDateWithoutDuplicate()
    {
        using var db = TestDbContextFactory.Create();
        var service = new RoutineTrainingDateService(db, NullLogger<RoutineTrainingDateService>.Instance);
        var date = new DateOnly(2026, 10, 4);

        var first = await service.AddAsync(date, 7);
        await service.RemoveAsync(date, 7);
        var restored = await service.AddAsync(date, 8);

        Assert.Equal(first.RoutineTrainingDateId, restored.RoutineTrainingDateId);
        Assert.Single(await service.ListAsync(date, date));
        Assert.Single(db.RoutineTrainingDates.IgnoreQueryFilters());
    }
}
