namespace PropertyManagement.Core.Common;

public interface ICreationAudited
{
    DateTime CreatedAt { get; }
    string CreatedById { get; }
}

public interface IAuditable : ICreationAudited
{
    DateTime? ModifiedAt { get; }
    string? ModifiedById { get; }
}

public abstract class CreationAuditedEntity : ICreationAudited
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedById { get; private set; } = null!;
}

public abstract class AuditableEntity : IAuditable
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedById { get; private set; }
}