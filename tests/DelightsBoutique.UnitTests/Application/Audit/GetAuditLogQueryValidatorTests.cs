using DelightsBoutique.Application.Audit.Queries;
using DelightsBoutique.Application.Audit.Validators;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Application.Audit;

public class GetAuditLogQueryValidatorTests
{
    private readonly GetAuditLogQueryValidator _validator = new();

    [Fact]
    public async Task Validate_PageZero_Fails()
    {
        var result = await _validator.ValidateAsync(new GetAuditLogQuery(null, 0, 20));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_LimitOver100_Fails()
    {
        var result = await _validator.ValidateAsync(new GetAuditLogQuery(null, 1, 101));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_Defaults_Pass()
    {
        var result = await _validator.ValidateAsync(new GetAuditLogQuery(null));

        result.IsValid.Should().BeTrue();
    }
}
