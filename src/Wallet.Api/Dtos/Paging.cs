namespace Wallet.Api.Dtos;

public class PageQuery
{
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;
}
