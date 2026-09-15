using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.Entities;
public class UnitType : AuditableEntity
{
    public const int NameMaxLength = 50;

    private UnitType()
    {
        Name = null!;
    }

    public UnitType(string name, bool isActive = true)
    {
        Name = Guard.Required(name, "Name", NameMaxLength);
        IsActive = isActive;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public void Rename(string name) => Name = Guard.Required(name, "Name", NameMaxLength);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}