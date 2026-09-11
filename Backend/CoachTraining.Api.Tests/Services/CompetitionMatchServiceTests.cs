using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.CompetitionMatches;
using CoachTraining.Api.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CompetitionMatchServiceTests
{
    [Fact]
    public void RequestValidation_RejectsBlankFieldsAndReversedDateRange()
    {
        var request = new CompetitionMatchRequestDto
        {
            Name = " ", Province = " ",
            StartDate = new DateOnly(2026, 10, 3), EndDate = new DateOnly(2026, 10, 1),
        };
        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Name)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Province)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.EndDate)));
    }

    [Fact]
    public async Task CreateUpdateAndDeleteAsync_PersistsExpectedState()
    {
        using var db = TestDbContextFactory.Create();
        var service = new CompetitionMatchService(db);
        var request = new CompetitionMatchRequestDto
        {
            Name = " Thailand Open ", Province = " Bangkok ",
            StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 10, 3),
        };

        var created = await service.CreateAsync(request, 1);
        request.Name = "Thailand Championship";
        var updated = await service.UpdateAsync(created.CompetitionMatchId, request, 1);
        var deleted = await service.DeleteAsync(created.CompetitionMatchId, 1);

        Assert.Equal("Thailand Open", created.Name);
        Assert.Equal("Bangkok", created.Province);
        Assert.Equal("Thailand Championship", updated!.Name);
        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.CompetitionMatchId));
    }

    [Fact]
    public async Task ListAsync_SearchesNameAndProvince()
    {
        using var db = TestDbContextFactory.Create();
        var service = new CompetitionMatchService(db);
        await service.CreateAsync(new CompetitionMatchRequestDto { Name = "รายการภาคเหนือ", Province = "เชียงใหม่", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 1) }, 1);

        var result = await service.ListAsync(new PagedRequest { Search = "เชียงใหม่" });

        Assert.Single(result.Items);
    }
}
