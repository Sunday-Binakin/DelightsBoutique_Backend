using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Application.Orders.Validators;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Application.Orders;

public class UpdateOrderStatusCommandValidatorTests
{
    private readonly UpdateOrderStatusCommandValidator _validator = new();

    [Fact]
    public async Task Validate_BothStatusAndNotesNull_Fails()
    {
        var result = await _validator.ValidateAsync(new UpdateOrderStatusCommand("DB-TEST", null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_StatusOnly_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateOrderStatusCommand("DB-TEST", OrderStatus.Confirmed, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NotesOnly_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateOrderStatusCommand("DB-TEST", null, "note"));

        result.IsValid.Should().BeTrue();
    }
}
