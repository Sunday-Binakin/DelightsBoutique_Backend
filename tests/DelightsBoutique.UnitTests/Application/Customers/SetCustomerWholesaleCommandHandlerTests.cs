using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Customers.Commands;
using DelightsBoutique.Application.Customers.Dtos;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Customers;

public class SetCustomerWholesaleCommandHandlerTests
{
    private readonly Mock<ICustomerFlagRepository> _customerFlags = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IPhoneNormalizer> _phoneNormalizer = new();

    private SetCustomerWholesaleCommandHandler CreateHandler() => new(_customerFlags.Object, _customers.Object, _phoneNormalizer.Object);

    [Fact]
    public async Task Handle_ExistingCustomer_UpsertsFlagAndReturnsAggregatedDto()
    {
        _phoneNormalizer.Setup(p => p.Normalize("+233241234567")).Returns("0241234567");
        var expected = new CustomerDto("0241234567", "Jane Doe", 3, 300m, DateTime.UtcNow, true);
        _customers.Setup(c => c.GetByPhoneAsync("0241234567", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateHandler().Handle(new SetCustomerWholesaleCommand("+233241234567", true), CancellationToken.None);

        result.Should().Be(expected);
        _customerFlags.Verify(f => f.UpsertAsync("0241234567", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PhoneWithNoOrders_ReturnsNull()
    {
        _phoneNormalizer.Setup(p => p.Normalize(It.IsAny<string>())).Returns("0241234567");
        _customers.Setup(c => c.GetByPhoneAsync("0241234567", It.IsAny<CancellationToken>())).ReturnsAsync((CustomerDto?)null);

        var result = await CreateHandler().Handle(new SetCustomerWholesaleCommand("0241234567", true), CancellationToken.None);

        result.Should().BeNull();
        _customerFlags.Verify(f => f.UpsertAsync("0241234567", true, It.IsAny<CancellationToken>()), Times.Once); // flag write still happens
    }
}
