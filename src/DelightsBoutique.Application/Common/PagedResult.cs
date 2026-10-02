namespace DelightsBoutique.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Data, int Page, int Limit, int Total);
