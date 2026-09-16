namespace PropertyManagement.Data.Services;

public class ServiceResult
{
    public string? Error { get; }

    public bool Succeeded => Error is null;

    protected ServiceResult(string? error)
    {
        Error = error;
    }

    public static ServiceResult Success()
    {
        return new ServiceResult(null);
    }

    public static ServiceResult Failure(string error)
    {
        return new ServiceResult(error);
    }
}
