using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.Validation;

namespace PropertyManagement.Core.Entities;

public class RentalApplication : AuditableEntity
{
    private readonly List<Applicant> _applicants = [];
    private readonly List<ResidenceHistory> _residences = [];
    private readonly List<StatusHistory> _statusHistory = [];
    public int Id { get; private set; }
    public int UnitId { get; private set; }
    public Unit Unit { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public string? ClaimedById { get; private set; }
    public DateTime? ClaimedAt { get; private set; }
    public DateTime? ResidenceSectionSavedAt { get; private set; }
    public Guid ResidenceSectionVersion { get; private set; }
    public Lease? Lease { get; private set; }
    public IReadOnlyCollection<Applicant> Applicants => _applicants;
    public IReadOnlyCollection<ResidenceHistory> Residences => _residences;
    public IReadOnlyCollection<StatusHistory> StatusHistory => _statusHistory;

    public bool IsEditable => Status.IsEditable();

    public bool AreApplicantDetailsComplete
    {
        get
        {
            if (_applicants.Count == 0)
                return false;

            return _applicants.All(a => a.AreDetailsComplete);
        }
    }

    public bool IsResidenceSectionComplete
    {
        get
        {
            if (ResidenceSectionSavedAt is null)
                return false;

            return GetResidenceSectionErrors().Count == 0;
        }
    }

    public bool CanSubmit
    {
        get
        {
            if (!IsEditable)
                return false;

            return AreApplicantDetailsComplete && IsResidenceSectionComplete;
        }
    }

    private RentalApplication()
    {
        Unit = null!;
    }

    public static RentalApplication Start(int unitId, string applicantUserId, bool unitHasActiveLease, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(applicantUserId))
            throw new DomainException("An applicant is required.");
        if (unitHasActiveLease)
            throw new DomainException("This unit is not available.");

        var application = new RentalApplication
        {
            UnitId = unitId,
            Status = ApplicationStatus.Draft,
            ResidenceSectionVersion = Guid.NewGuid()
        };

        application._applicants.Add(new Applicant(applicantUserId, isPrimary: true));
        application._statusHistory.Add(new StatusHistory(null, ApplicationStatus.Draft, applicantUserId, now, null));
        return application;
    }

    public bool IsApplicant(string userId)
    {
        return _applicants.Any(a => a.UserId == userId);
    }

    public Applicant AddApplicant(string actingUserId, string newApplicantUserId)
    {
        EnsureEditableBy(actingUserId);
        if (IsApplicant(newApplicantUserId))
            throw new DomainException("That person is already on this application.");

        var applicant = new Applicant(newApplicantUserId, isPrimary: false);
        _applicants.Add(applicant);
        return applicant;
    }

    public void SaveApplicantDetails(string actingUserId, string applicantUserId, ApplicantDetails details, DateTime now, bool allowInvalid = false)
    {
        EnsureEditableBy(actingUserId);

        var applicant = _applicants.SingleOrDefault(a => a.UserId == applicantUserId);
        if (applicant is null)
            throw new DomainException("That person is not on this application.");

        applicant.SaveDetails(details, allowInvalid, now);
    }

    public ResidenceHistory AddResidence(string actingUserId, ResidenceDetails details)
    {
        EnsureEditableBy(actingUserId);

        var residence = new ResidenceHistory(details);
        _residences.Add(residence);
        ResidenceSectionVersion = Guid.NewGuid();
        return residence;
    }

    public void UpdateResidence(string actingUserId, int residenceId, ResidenceDetails details)
    {
        EnsureEditableBy(actingUserId);

        FindResidence(residenceId).Apply(details);
        ResidenceSectionVersion = Guid.NewGuid();
    }

    public void RemoveResidence(string actingUserId, int residenceId)
    {
        EnsureEditableBy(actingUserId);

        _residences.Remove(FindResidence(residenceId));
        ResidenceSectionVersion = Guid.NewGuid();
    }

    public IReadOnlyList<FieldError> GetResidenceSectionErrors()
    {
        if (ResidenceSectionSavedAt is null)
            return [];

        return ResidenceSectionRules.Validate(_residences.Count);
    }

    public void SaveResidenceSection(string actingUserId, DateTime now, bool allowInvalid = false)
    {
        EnsureEditableBy(actingUserId);

        var errors = ResidenceSectionRules.Validate(_residences.Count);
        if (errors.Count > 0 && !allowInvalid)
            throw new DomainException(errors[0].Message);

        ResidenceSectionSavedAt = now;
        ResidenceSectionVersion = Guid.NewGuid();
    }

    public void Submit(string actingUserId, bool unitHasActiveLease, DateTime now)
    {
        EnsureEditableBy(actingUserId);

        if (!AreApplicantDetailsComplete)
            throw new DomainException("Applicant information must be saved, without errors, for every applicant before submitting.");
        if (!IsResidenceSectionComplete)
            throw new DomainException("Residence history must be saved, without errors, before submitting.");
        if (unitHasActiveLease)
            throw new DomainException("This unit already has an active lease.");

        SubmittedAt = now;
        ChangeStatus(ApplicationStatus.Submitted, actingUserId, now, comment: null);
    }

    public void Withdraw(string actingUserId, DateTime now)
    {
        EnsureApplicant(actingUserId);
        if (Status.IsTerminal())
            throw new DomainException($"This application is already {Status.DisplayName().ToLowerInvariant()}.");

        ClearClaim();
        ChangeStatus(ApplicationStatus.Withdrawn, actingUserId, now, comment: null);
    }

    public void Claim(string managerId, DateTime now)
    {
        if (Status != ApplicationStatus.Submitted)
            throw new DomainException("Only submitted applications can be claimed.");

        ClaimedById = managerId;
        ClaimedAt = now;
        ChangeStatus(ApplicationStatus.UnderReview, managerId, now, comment: null);
    }

    public void Release(string managerId, DateTime now)
    {
        EnsureClaimedBy(managerId);

        ClearClaim();
        ChangeStatus(ApplicationStatus.Submitted, managerId, now, comment: null);
    }

    public Lease Approve(string managerId, DateOnly leaseStartDate, DateOnly today, bool unitHasConflictingLease, string? comment, DateTime now)
    {
        EnsureClaimedBy(managerId);
        Lease.EnsureValidStartDate(leaseStartDate, today);
        if (unitHasConflictingLease)
            throw new DomainException("This unit already has an active or overlapping lease.");

        var lease = new Lease(UnitId, leaseStartDate);
        Lease = lease;
        ClearClaim();
        ChangeStatus(ApplicationStatus.Approved, managerId, now, Entities.StatusHistory.OptionalComment(comment));
        return lease;
    }

    public void Return(string managerId, string comment, DateTime now)
    {
        EnsureClaimedBy(managerId);

        ClearClaim();
        ChangeStatus(ApplicationStatus.Returned, managerId, now, Entities.StatusHistory.RequiredComment(comment));
    }

    public void Deny(string managerId, string comment, DateTime now)
    {
        EnsureClaimedBy(managerId);

        ClearClaim();
        ChangeStatus(ApplicationStatus.Denied, managerId, now, Entities.StatusHistory.RequiredComment(comment));
    }

    private void ChangeStatus(ApplicationStatus to, string userId, DateTime now, string? comment)
    {
        _statusHistory.Add(new StatusHistory(Status, to, userId, now, comment));
        Status = to;
    }

    private void EnsureApplicant(string userId)
    {
        if (!IsApplicant(userId))
            throw new DomainException("You are not an applicant on this application.");
    }

    private void EnsureEditableBy(string userId)
    {
        EnsureApplicant(userId);
        if (!IsEditable)
            throw new DomainException("This application can only be changed while it is a draft or has been returned.");
    }

    private void EnsureClaimedBy(string managerId)
    {
        if (Status != ApplicationStatus.UnderReview)
            throw new DomainException("The application must be claimed for review first.");
        if (ClaimedById != managerId)
            throw new DomainException("This application is claimed by another property manager.");
    }

    private void ClearClaim()
    {
        ClaimedById = null;
        ClaimedAt = null;
    }

    private ResidenceHistory FindResidence(int residenceId)
    {
        var residence = _residences.SingleOrDefault(r => r.Id == residenceId);
        if (residence is null)
            throw new DomainException("Residence not found.");

        return residence;
    }
}
