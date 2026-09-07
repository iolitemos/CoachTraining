using CoachTraining.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoachTraining.Api.Tests;

public static class TestDbContextFactory
{
    /// <summary>A fresh isolated in-memory ApplicationDbContext for a single test.</summary>
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // The in-memory provider doesn't support transactions; services are
            // allowed to open them (for real-provider atomicity) and this just
            // makes InMemoryDatabase.BeginTransactionAsync a harmless no-op in tests.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
