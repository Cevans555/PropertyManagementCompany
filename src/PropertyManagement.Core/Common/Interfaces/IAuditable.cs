namespace PropertyManagement.Core.Common.Interfaces;

public interface IAuditable : ICreationAudited
{
    DateTime? ModifiedAt { get; }
    string? ModifiedById { get; }
}
