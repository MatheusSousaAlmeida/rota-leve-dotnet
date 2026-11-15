namespace RotaLeve.Api.Domain.Models;
public record OperationResult<T>(bool IsSuccess, int StatusCode, T? Value, string? Error)
{
    public static OperationResult<T> Success(T value, int statusCode = 200) => new(true, statusCode, value, null);
    public static OperationResult<T> Failure(string error, int statusCode) => new(false, statusCode, default, error);
}
