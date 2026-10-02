using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using FluentAssertions;
using MediatR;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Common;

public class AuditLoggingBehaviorTests
{
    private readonly Mock<IAuditLogRepository> _auditLog = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private AuditLoggingBehavior<TestAuditableCommand, string> CreateBehavior() => new(_auditLog.Object, _currentUser.Object);
    private AuditLoggingBehavior<TestPlainCommand, string> CreatePlainBehavior() => new(_auditLog.Object, _currentUser.Object);

    public record TestAuditableCommand : IRequest<string>, IAuditableCommand
    {
        public string DescribeAction() => "Did a thing";
    }

    public record TestPlainCommand : IRequest<string>;

    [Fact]
    public async Task Handle_AuditableCommand_WritesAuditEntryAfterSuccess()
    {
        _currentUser.Setup(u => u.Email).Returns("owner@example.com");

        var result = await CreateBehavior().Handle(new TestAuditableCommand(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        _auditLog.Verify(a => a.AddAsync("owner@example.com", "Did a thing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonAuditableCommand_NeverWritesAuditEntry()
    {
        await CreatePlainBehavior().Handle(new TestPlainCommand(), _ => Task.FromResult("ok"), CancellationToken.None);

        _auditLog.Verify(a => a.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HandlerThrows_NeverWritesAuditEntry()
    {
        var act = () => CreateBehavior().Handle(
            new TestAuditableCommand(), _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _auditLog.Verify(a => a.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoEmail_FallsBackToName_ThenUnknown()
    {
        _currentUser.Setup(u => u.Email).Returns((string?)null);
        _currentUser.Setup(u => u.Name).Returns("Staff Name");

        await CreateBehavior().Handle(new TestAuditableCommand(), _ => Task.FromResult("ok"), CancellationToken.None);

        _auditLog.Verify(a => a.AddAsync("Staff Name", "Did a thing", It.IsAny<CancellationToken>()), Times.Once);
    }
}
