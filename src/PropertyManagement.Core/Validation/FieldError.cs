namespace PropertyManagement.Core.Validation;

public sealed record FieldError(string Field, string Message, bool BlocksSaving = false);