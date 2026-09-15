using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.Entities;

public class ManagerNote : AuditableEntity
{
    public const int TextMaxLength = 2000;

    public int Id { get; private set; }
    public int RentalApplicationId { get; private set; }
    public string Text { get; private set; }

    private ManagerNote()
    {
        Text = null!;
    }

    internal ManagerNote(string text)
    {
        Text = Guard.Required(text, "Note", TextMaxLength);
    }

    internal void Edit(string text)
    {
        Text = Guard.Required(text, "Note", TextMaxLength);
    }
}
