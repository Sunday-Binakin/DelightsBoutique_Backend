using DelightsBoutique.Application.Reports.Queries;
using DelightsBoutique.Application.Reports.Validators;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Application.Reports;

public class GetReportsQueryValidatorTests
{
    private readonly GetReportsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_ToBeforeFrom_Fails()
    {
        var query = new GetReportsQuery(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), null, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ToOnOrAfterFrom_Passes()
    {
        var query = new GetReportsQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NoDatesGiven_Passes()
    {
        var result = await _validator.ValidateAsync(new GetReportsQuery(null, null, null, null, null));

        result.IsValid.Should().BeTrue();
    }
}
