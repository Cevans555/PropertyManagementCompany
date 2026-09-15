namespace PropertyManagement.Core.Common;

internal static class Guard
{
    public static string Required(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");

        return trimmed;
    }
}