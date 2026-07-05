namespace Project.Business.Results;

public class ServiceResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();

    public static ServiceResult Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static ServiceResult Fail(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? Enumerable.Empty<string>() };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static new ServiceResult<T> Fail(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? Enumerable.Empty<string>() };
}
