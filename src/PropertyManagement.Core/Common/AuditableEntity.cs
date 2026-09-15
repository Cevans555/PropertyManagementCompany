using PropertyManagement.Core.Common.Interfaces;

namespace PropertyManagement.Core.Common;

public abstract class AuditableEntity : IAuditable
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedById { get; private set; }
}