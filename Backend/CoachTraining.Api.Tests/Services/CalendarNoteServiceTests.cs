using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CalendarNoteServiceTests
{
    [Fact]
    public async Task UpsertAsync_CreatesThenUpdatesOneNotePerDate()
    {
        using var db = TestDbContextFactory.Create();
        var service = new CalendarNoteService(db, NullLogger<CalendarNoteService>.Instance);
        var date = new DateOnly(2026, 9, 13);

        var created = await service.UpsertAsync(date, "  แจ้งหยุดฝึก  ", 1);
        var updated = await service.UpsertAsync(date, "เปลี่ยนเวลาเริ่ม", 2);
        var notes = await service.ListAsync(date, date);

        Assert.Equal(created.CalendarNoteId, updated.CalendarNoteId);
        Assert.Equal("แจ้งหยุดฝึก", created.Content);
        Assert.Equal("เปลี่ยนเวลาเริ่ม", Assert.Single(notes).Content);
        Assert.Single(db.CalendarNotes);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesNoteFromCalendarRange()
    {
        using var db = TestDbContextFactory.Create();
        var service = new CalendarNoteService(db, NullLogger<CalendarNoteService>.Instance);
        var date = new DateOnly(2026, 9, 14);
        await service.UpsertAsync(date, "วันหยุด", 1);

        var deleted = await service.DeleteAsync(date, 1);

        Assert.True(deleted);
        Assert.Empty(await service.ListAsync(date, date));
        Assert.True(db.CalendarNotes.IgnoreQueryFilters().Single().IsDeleted);
    }
}
