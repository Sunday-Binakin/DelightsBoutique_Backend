namespace DelightsBoutique.Application.Common;

/// <summary>
/// Marker for commands that should write an AuditEntry after a successful write. Picked
/// up automatically by <see cref="AuditLoggingBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>Human-readable action string built from data already on the command —
    /// no dependency on the handler's result.</summary>
    string DescribeAction();
}
