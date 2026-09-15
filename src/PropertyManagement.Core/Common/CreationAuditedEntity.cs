using PropertyManagement.Core.Common.Interfaces;

namespace PropertyManagement.Core.Common;

public abstract class CreationAuditedEntity : ICreationAudited
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedById { get; private set; } = null!;
}
