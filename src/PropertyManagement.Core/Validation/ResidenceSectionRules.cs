namespace PropertyManagement.Core.Validation;

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
