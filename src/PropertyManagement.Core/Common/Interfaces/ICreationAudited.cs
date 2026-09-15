namespace PropertyManagement.Core.Common.Interfaces;

public interface ICreationAudited
{
    DateTime CreatedAt { get; }
    string CreatedById { get; }
}

