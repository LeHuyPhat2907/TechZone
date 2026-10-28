namespace TechZone.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = new();

    public static Result Success(string message = "") => new() { Succeeded = true, Message = message };
    public static Result Failure(string error) => new() { Succeeded = false, Errors = new() { error }, Message = error };
    public static Result Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    public static Result<T> Success(T data, string message = "") => new() { Succeeded = true, Data = data, Message = message };
    public new static Result<T> Failure(string error) => new() { Succeeded = false, Errors = new() { error }, Message = error };
    public new static Result<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, long totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
