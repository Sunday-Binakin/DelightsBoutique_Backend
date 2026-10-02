using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using FluentAssertions;
using MediatR;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Common;

public class IdempotencyBehaviorTests
{
    private readonly Mock<IIdempotencyStore> _store = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public IdempotencyBehaviorTests()
    {
        // Pass-through fake: just invokes the given delegate directly, no real transaction.
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> action, CancellationToken ct) => action(ct));
    }

    private IdempotencyBehavior<TRequest, string> CreateBehavior<TRequest>() where TRequest : IRequest<string> =>
        new(_store.Object, _unitOfWork.Object);

    public record PlainCommand : IRequest<string>;
    public record IdempotentCommand(string IdempotencyKey, string Payload) : IRequest<string>, IIdempotentCommand;

    [Fact]
    public async Task Handle_NonIdempotentCommand_PassesThroughDirectly_NeverTouchesStore()
    {
        var result = await CreateBehavior<PlainCommand>().Handle(new PlainCommand(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        _store.Verify(s => s.FindAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FreshKey_RunsHandlerAndSavesRecord()
    {
        _store.Setup(s => s.FindAsync("key-1", "IdempotentCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyKeyLookup?)null);

        var result = await CreateBehavior<IdempotentCommand>().Handle(
            new IdempotentCommand("key-1", "payload"), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        _store.Verify(s => s.SaveAsync("key-1", "IdempotentCommand", It.IsAny<string>(), 200, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameKeySameBody_ReturnsCachedResponse_NeverInvokesNext()
    {
        var command = new IdempotentCommand("key-1", "payload");
        var hash = RequestHasher.Hash(command);
        _store.Setup(s => s.FindAsync("key-1", "IdempotentCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyKeyLookup(hash, 200, "\"cached-response\""));

        var nextCalled = false;
        var result = await CreateBehavior<IdempotentCommand>().Handle(command, _ => { nextCalled = true; return Task.FromResult("ok"); }, CancellationToken.None);

        result.Should().Be("cached-response");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SameKeyDifferentBody_ThrowsIdempotencyConflictException_NeverInvokesNext()
    {
        _store.Setup(s => s.FindAsync("key-1", "IdempotentCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyKeyLookup("a-different-hash", 200, "\"cached-response\""));

        var nextCalled = false;
        var act = () => CreateBehavior<IdempotentCommand>().Handle(
            new IdempotentCommand("key-1", "payload"), _ => { nextCalled = true; return Task.FromResult("ok"); }, CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyConflictException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HandlerThrows_NeverCallsSaveAsync()
    {
        _store.Setup(s => s.FindAsync("key-1", "IdempotentCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyKeyLookup?)null);

        var act = () => CreateBehavior<IdempotentCommand>().Handle(
            new IdempotentCommand("key-1", "payload"), _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _store.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
