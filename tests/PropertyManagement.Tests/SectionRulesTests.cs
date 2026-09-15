using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Validation;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;

public class SectionRulesTests
{
    [Fact]
    public void ValidDetails_HaveNoErrors()
    {
        Assert.Empty(ApplicantDetailsRules.Validate(Details()));
    }

    [Fact]
    public void MissingValues_ReportOneErrorPerField_NoneBlockingSaving()
    {
        var errors = ApplicantDetailsRules.Validate(Details(null, "", " ", null, null, null, null, null));

        Assert.Equal(
            ["FirstName", "LastName", "Phone", "Email", "Street", "City", "State", "PostalCode"],
            errors.Select(e => e.Field));
        Assert.Contains(errors, e => e.Message == "First name is required.");
        Assert.DoesNotContain(errors, e => e.BlocksSaving);
    }

    [Theory]
    [InlineData("555-12")]
    [InlineData("call me")]
    public void InvalidPhone_IsReportedOnPhone(string phone)
    {
        var error = Assert.Single(ApplicantDetailsRules.Validate(Details(phone: phone)));

        Assert.Equal(new FieldError("Phone", "Phone must be a valid phone number."), error);
    }

    [Theory]
    [InlineData("jane")]
    [InlineData("jane@example")]
    [InlineData("jane doe@example.com")]
    public void InvalidEmail_IsReportedOnEmail(string email)
    {
        var error = Assert.Single(ApplicantDetailsRules.Validate(Details(email: email)));

        Assert.Equal("Email", error.Field);
    }

    [Fact]
    public void ValueTooLongToStore_BlocksSaving()
    {
        var tooLong = new string('a', Applicant.NameMaxLength + 1);

        var error = Assert.Single(ApplicantDetailsRules.Validate(Details(firstName: tooLong)));

        Assert.True(error.BlocksSaving);
        Assert.Equal("First name must be 100 characters or fewer.", error.Message);
    }

    [Fact]
    public void SaveApplicantDetails_WithErrors_ThrowsByDefault()
    {
        var application = Draft();

        Assert.Throws<DomainException>(() =>
            application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(phone: null), Now));
        Assert.False(application.Applicants.Single().HasSavedDetails);
    }

    [Fact]
    public void SaveApplicantDetails_AllowInvalid_SavesPartialDetails_ButSubmitStaysBlockedUntilFixed()
    {
        var application = Draft();
        application.AddResidence(ApplicantId, Residence());
        application.SaveResidenceSection(ApplicantId, Now);

        application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(phone: null, city: "  "), Now, allowInvalid: true);

        var applicant = application.Applicants.Single();
        Assert.True(applicant.HasSavedDetails);
        Assert.Null(applicant.Phone);
        Assert.Null(applicant.City);
        Assert.Equal(["Phone", "City"], applicant.GetDetailsErrors().Select(e => e.Field));
        Assert.False(application.CanSubmit);
        Assert.Throws<DomainException>(() => application.Submit(ApplicantId, unitHasActiveLease: false, Now));

        application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(), Now);

        Assert.Empty(applicant.GetDetailsErrors());
        Assert.True(application.CanSubmit);
    }

    [Fact]
    public void SaveApplicantDetails_AllowInvalid_StillRejectsValuesTooLongToStore()
    {
        var application = Draft();
        var tooLongEmail = new string('a', 300) + "@example.com";

        Assert.Throws<DomainException>(() => application.SaveApplicantDetails(
            ApplicantId, ApplicantId, Details(email: tooLongEmail), Now, allowInvalid: true));
    }

    [Fact]
    public void SaveResidenceSection_WithoutResidences_ThrowsByDefault_AndSavesWithErrorWhenAllowed()
    {
        var application = Draft();
        application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(), Now);

        Assert.Throws<DomainException>(() => application.SaveResidenceSection(ApplicantId, Now));

        application.SaveResidenceSection(ApplicantId, Now, allowInvalid: true);

        Assert.NotNull(application.ResidenceSectionSavedAt);
        Assert.Equal("Add at least one prior residence.", Assert.Single(application.GetResidenceSectionErrors()).Message);
        Assert.Throws<DomainException>(() => application.Submit(ApplicantId, unitHasActiveLease: false, Now));
    }
}
