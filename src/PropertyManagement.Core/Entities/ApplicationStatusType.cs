using PropertyManagement.Core.Enums;

namespace PropertyManagement.Core.Entities;

public class ApplicationStatusType
{
    private ApplicationStatusType()
    {
        Name = null!;
    }

    public ApplicationStatus Id { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsTerminal { get; private set; }

    public static IReadOnlyList<ApplicationStatusType> FromEnum() =>
        Enum.GetValues<ApplicationStatus>()
            .Select(status => new ApplicationStatusType
            {
                Id = status,
                Name = status.DisplayName(),
                SortOrder = (int)status,
                IsTerminal = status.IsTerminal()
            })
            .ToList();
}