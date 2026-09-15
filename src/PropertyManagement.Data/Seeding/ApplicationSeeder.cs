using Bogus;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;

namespace PropertyManagement.Data.Seeding;

internal static class ApplicationSeeder
{
    private const int ApplicationsPerStatus = 3;

    public static async Task SeedAsync(
        PropertyManagementDbContext db,
        Faker faker,
        List<Property> properties,
        List<AppUser> managers,
        List<AppUser> applicants,
        DateTime now,
        SeedAuditStamps audit,
        CancellationToken cancellationToken)
    {
        var units = faker.Random.Shuffle(properties.SelectMany(p => p.Units)).ToList();
        var leasedUnits = units.Take(ApplicationsPerStatus).ToList();
        var openUnits = units.Skip(ApplicationsPerStatus).ToList();

        var pendingNotes = new List<(RentalApplication Application, string ManagerId, DateTime At)>();

        var applicantIndex = 0;
        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            for (var i = 0; i < ApplicationsPerStatus; i++)
            {
                Unit unit;
                if (status == ApplicationStatus.Approved)
                    unit = leasedUnits[i];
                else
                    unit = faker.PickRandom(openUnits);

                var applicant = applicants[applicantIndex % applicants.Count];
                applicantIndex++;

                AppUser? coApplicant = null;
                if (i == 0 && status != ApplicationStatus.Draft)
                    coApplicant = applicants[(applicantIndex + applicants.Count / 2) % applicants.Count];

                var manager = faker.PickRandom(managers);
                var leaveIncomplete = status == ApplicationStatus.Draft && i == 0;

                var application = BuildApplication(faker, unit, status, applicant, coApplicant, manager, now, leaveIncomplete, audit, out var claimedAt);
                db.RentalApplications.Add(application);
                audit.ApplyTo(db);

                if (claimedAt is not null)
                    pendingNotes.Add((application, manager.Id, claimedAt.Value));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (application, managerId, at) in pendingNotes)
        {
            var note = new ManagerNote(application.Id, faker.PickRandom(FakeData.ManagerNotes));
            db.ManagerNotes.Add(note);
            audit.Add(note, managerId, at);
        }

        audit.ApplyTo(db);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RentalApplication BuildApplication(
        Faker faker,
        Unit unit,
        ApplicationStatus target,
        AppUser applicant,
        AppUser? coApplicant,
        AppUser manager,
        DateTime now,
        bool leaveIncomplete,
        SeedAuditStamps audit,
        out DateTime? claimedAt)
    {
        claimedAt = null;
        var time = now.AddDays(-faker.Random.Int(20, 90));

        DateTime Next()
        {
            time = time.AddHours(faker.Random.Int(1, 48));
            return time;
        }

        var application = RentalApplication.Start(unit.Id, applicant.Id, unitHasActiveLease: false, time);
        audit.Add(application, applicant.Id, time);
        audit.Add(application.Applicants.Single(), applicant.Id, time);
        application.SaveApplicantDetails(applicant.Id, applicant.Id, FakeData.ApplicantDetails(faker, applicant), Next());

        if (leaveIncomplete)
            return application;

        if (coApplicant is not null)
        {
            var added = application.AddApplicant(applicant.Id, coApplicant.Id);
            audit.Add(added, applicant.Id, Next());
            application.SaveApplicantDetails(coApplicant.Id, coApplicant.Id, FakeData.ApplicantDetails(faker, coApplicant), Next());
        }

        AddResidences(faker, application, applicant, time, audit, Next);
        application.SaveResidenceSection(applicant.Id, Next());

        if (target == ApplicationStatus.Draft)
            return application;

        application.Submit(applicant.Id, unitHasActiveLease: false, Next());

        if (target == ApplicationStatus.Submitted)
            return application;

        if (target == ApplicationStatus.Withdrawn)
        {
            application.Withdraw(applicant.Id, Next());
            return application;
        }

        claimedAt = Next();
        application.Claim(manager.Id, claimedAt.Value);

        switch (target)
        {
            case ApplicationStatus.Approved:
                var approvedAt = Next();
                var today = DateOnly.FromDateTime(approvedAt);
                var lease = application.Approve(manager.Id, today, today, unitHasConflictingLease: false, "Welcome! Your lease is ready to sign.", approvedAt);
                audit.Add(lease, manager.Id, approvedAt);
                break;
            case ApplicationStatus.Returned:
                application.Return(manager.Id, faker.PickRandom(FakeData.ReturnComments), Next());
                break;
            case ApplicationStatus.Denied:
                application.Deny(manager.Id, faker.PickRandom(FakeData.DenyComments), Next());
                break;
        }

        return application;
    }

    private static void AddResidences(
        Faker faker, RentalApplication application, AppUser applicant, DateTime time, SeedAuditStamps audit, Func<DateTime> next)
    {
        var moveOut = DateOnly.FromDateTime(time).AddMonths(-faker.Random.Int(0, 2));
        var count = faker.Random.Int(1, 3);

        for (var i = 0; i < count; i++)
        {
            var moveIn = moveOut.AddMonths(-faker.Random.Int(10, 36));
            var details = new ResidenceDetails
            {
                Address = FakeData.Address(faker),
                LandlordName = faker.Name.FullName(),
                LandlordPhone = FakeData.Phone(faker),
                MoveInDate = moveIn,
                MoveOutDate = moveOut
            };

            var residence = application.AddResidence(applicant.Id, details);
            audit.Add(residence, applicant.Id, next());
            moveOut = moveIn.AddDays(-1);
        }
    }
}
