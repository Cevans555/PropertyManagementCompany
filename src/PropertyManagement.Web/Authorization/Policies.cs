namespace PropertyManagement.Web.Authorization;

/// <summary>Policy names for [Authorize(Policy = ...)].</summary>
public static class Policies
{
    public const string PropertyManager = nameof(PropertyManager);
    public const string Applicant = nameof(Applicant);
}
