namespace Kaan.SecurityPlatform.Application.Common.Models;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static Result<T> Success(T value) => new(true, value, null, null, null);
    public static Result<T> Failure(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage, null);
    public static Result<T> ValidationFailure(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, default, "validation_error", "Doğrulama hatası oluştu.", errors);
}

public sealed class Result
{
    private Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}
