namespace PropertyManagement.Core.Security;

public static class Roles
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "PropertyManager";

    public static readonly IReadOnlyList<string> All = [Applicant, PropertyManager];
}