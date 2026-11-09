namespace Product.Application.Common.DTOs;

public sealed record PagedResultDTO<T>(IReadOnlyList<T> Items, int? NextCursor, bool HasMore);
