namespace SGInsurance.Application.Common;

/// <summary>Standard response envelope used by every v1 endpoint.</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Data = default,
        Message = message,
        Errors = errors ?? new List<string>()
    };
}

public static class ApiResponse
{
    public static ApiResponse<object?> Ok(string? message = null) => new() { Success = true, Data = null, Message = message };
}
