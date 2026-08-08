namespace Kaan.SecurityPlatform.Application.Common.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public sealed record PagedQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Sort = null);
