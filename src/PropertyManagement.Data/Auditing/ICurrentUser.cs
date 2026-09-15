namespace PropertyManagement.Data.Auditing;

public interface ICurrentUser
{
    string? UserId { get; }
}
