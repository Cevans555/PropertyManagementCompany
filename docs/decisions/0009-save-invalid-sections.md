# 0009: The save-invalid switch changes what may be saved, never what is valid

## Context
Bonus 4 asks that a section can be saved even when it fails validation, with the errors shown. The Summary lists everything still blocking submission, and Submit stays blocked while any error remains. Rules are defined once per section, and each error is attached to the field it belongs to. The base specification says the opposite: Continue saves only a valid section. Both behaviours need to exist.

## Decision
- Behind a feature switch, `Features:SaveInvalidSections`, off by default so the app behaves exactly as the base specification describes. It's read per request through `IOptionsSnapshot`, so changing it needs no restart.
- **The rules are defined once, in `Core/Validation`.** `ApplicantDetailsRules` and `ResidenceSectionRules` return `FieldError(Field, Message, BlocksSaving)`. The form, the entity, the Summary's blocker list and Submit all use these same rules.
- **The switch only decides whether a section with errors may be saved.** Validity is recalculated from the saved data every time. When the Summary is built and when Submit runs, the rules run again, so an application saved with errors can't be submitted, even after the switch is turned back off.
- **`BlocksSaving` errors can never be saved.** A value too long for its database column is rejected by the entity whatever the switch says, since the database couldn't store it anyway.
- Errors map back to fields by name, so returning to a section shows each message under its input.

## Alternatives considered
- **Storing an "is valid" flag when saving.** It could go stale when the rules or the switch change. Recalculating can't.
- **A separate draft copy of the form data.** A second storage path for the same fields, with no benefit.
- **Always on.** That would change the base behaviour the specification describes.

## Consequences
- Applicant address fields are stored as separate nullable columns rather than the `Address` value object, because a half-completed address has to be storable and `Address` refuses to be created incomplete. A comment in `Applicant.cs` records this.
- Tests cover the switch in both states, including `SectionSavedWithErrors_StillBlocksSubmit_AfterSwitchIsTurnedOff` and `SaveInvalidSections_ValueTooLongToStore_IsStillRejected`.
