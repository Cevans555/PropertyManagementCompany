# 0007: Authorization in three layers, and not-found instead of forbidden

## Context
There are two roles. Applicants may only touch applications they're on, which includes co-applicants (bonus 5). Managers see all applications but only review ones they've claimed (bonus 2). The specification asks that "permissions in the controllers and the UI reflect this".

## Decision
1. **A fallback policy** requires an authenticated user everywhere. Only the home page, the account pages, static files and, outside production, the OpenAPI document and Scalar page opt out.
2. **Role policies** (`PropertyManager`, `Applicant`) sit on controllers and actions. Properties, units, reviews and notes are manager-only, and starting an application is applicant-only.
3. **A resource-based handler**, `RentalApplicationAuthorizationHandler`, decides each operation on a specific application: `View`, `Edit`, `Withdraw`, `Review` and `ManageNotes`. For example, `Edit` requires the user to be an applicant on the application and the application to be Draft or Returned.

`ApplicationPageBuilder.AuthorizeAsync` loads the application and checks `View` first. **If the user can't even view it, the response is 404, not 403**, so an applicant can't discover which application ids exist. If they can view it but not perform the operation, the response is 403.

The same checks drive the UI through flags like `CanEdit`, `CanWithdraw`, `CanReview` and `CanManageNotes`, so a button never appears for an action the server would refuse.

**Authorization and domain rules are separate.** The handler answers "may this user attempt this?". The entity still answers "is it valid now?", for example whether this manager holds the claim or whether the sections are complete. Rules aren't duplicated into the handler.

Also:
- Antiforgery tokens are checked on every POST (`AutoValidateAntiforgeryTokenAttribute`).
- A login return URL is followed only if it's local.
- `/api` paths return 401 and 403 instead of redirecting, so the grid's `fetch` doesn't receive the login page as a 200.

## Alternatives considered
- **Checks written inline in each action.** Easy to miss one, and it's hard to see the whole rule set at once.
- **Returning 403 for other people's applications.** This reveals that the id exists.

## Consequences
- The rules are in one handler with unit tests, and integration tests prove the HTTP results, such as `Details_IsNotFoundForAnotherApplicant`, `Claim_ByApplicant_IsDenied` and `CreateUnit_PostedByApplicant_IsDenied`.
- The handler is a singleton because it's stateless and has no scoped dependencies.
