namespace PropertyManagement.Core.Validation;

/// <summary>The Residence History section rules. Each residence is validated when it's added through its modal.</summary>
public static class ResidenceSectionRules
{
    public const string ResidencesField = "Residences";

    public static IReadOnlyList<FieldError> Validate(int residenceCount)
    {
        if (residenceCount == 0)
            return [new FieldError(ResidencesField, "Add at least one prior residence.")];

        return [];
    }
}
