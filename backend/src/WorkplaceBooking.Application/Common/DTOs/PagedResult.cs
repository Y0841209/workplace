namespace WorkplaceBooking.Application.Common.DTOs;

public record PaginationRequest(
    int Page = 1,
    int PageSize = 20)
{
    public int ValidatedPage => Math.Max(1, Page);
    public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100);
}

public record PaginationResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
        new(Array.Empty<T>(), 0, page, pageSize);
    
    public PaginationResponse ToPaginationResponse() =>
        new(Page, PageSize, TotalCount, TotalPages);
}