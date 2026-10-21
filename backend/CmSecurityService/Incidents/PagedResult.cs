namespace CmSecurityService.Incidents;

/// <summary>
/// One page of results plus the information a caller needs to request the next page.
/// </summary>
/// <typeparam name="T">Type of the items in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>Items on this page.</summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>Current page number, starting at 1.</summary>
    public int Page { get; set; }

    /// <summary>Maximum number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of items matching the filters, across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Total number of pages for the current filters.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
