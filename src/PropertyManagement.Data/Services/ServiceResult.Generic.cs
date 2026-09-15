namespace PropertyManagement.Data.Services;

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Value { get; }

    private ServiceResult(T? value, string? error)
        : base(error)
    {
        Value = value;
    }

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(value, null);
    }

    public static new ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>(default, error);
    }
}
