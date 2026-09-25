---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Server.Admin.Services.Core.Account.AccountLinkBuilder      # new
  - Server.Admin.Services.Core.Account.IAccountLinkBuilder     # new
  - Server.Admin.Services.Core.Account.AccountRoutes           # new
  - Server.Admin.Services.Core.Account.UserActivation          # new
  - Server.Admin.Services.Core.UserAdmin.UserProfileService
  - Server.Admin.Services.Core.UserAdmin.IUserProfileService
  - Server.Admin.Services.Core.AccountService
  - Server.Admin.Services.Core.Background.Tasks.Handlers.ImportDataTaskHandler
  - Shared.Model.Core.UserAdmin.UserAccessStateItem            # new
  - Shared.Model.Core.UserAdmin.UserStatusLabels               # new
  - Shared.Model.Core.UserProfileItem
  - Server.Database.Core.Data.Decorators.UserProfileDecorators
  - Shared.Model.Core.Account.ConfirmEmailResult
  - Server.Admin.WebService.Core.UI.Pages.Admin.Users
  - Server.Admin.WebService.Core.UI.Pages.Admin.NewUser
  - Server.Admin.WebService.Core.UI.Pages.Admin.EditUser
  - Server.Admin.WebService.Core.UI.Pages.Account.ConfirmEmail
  - Server.Admin.WebService.Core.UI.Pages.Account.ForgotPassword
  - Server.Admin.WebService.Program                            # DI registration only (root, unmarked)
  - Server.Admin.Services.Mocks.Core.UserAdmin.UserProfileServiceMock
  - Server.Admin.Services.Core.Organizations.IOrganizationService  # DevCoreApp only: GetCurrentAsync was missing here
  - Server.Admin.Services.Core.Organizations.OrganizationService   # DevCoreApp only: GetCurrentAsync was missing here
status: applied
change: "24f54d500dc3d6bb3d93d1ca7d3b9d1e9e22a0b1"   # ThreadIQ main, "#1124: New user experience"
related:
  - docs/open-items.md   # ThreadIQ-local: the three neighbouring problems deliberately left alone
---

# An administrator-created user cannot sign in, is scoped to every organization, and never leaves INITIATED

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-08-28 | Authored in ThreadIQ `out/`. Not yet delivered. |
| 2026-08-28 | ThreadIQ change committed as `24f54d5` (`#1124`), 30 files. `change` filled in immediately afterwards, so that SHA carries this doc with the placeholder still in it — read the doc from ThreadIQ `main`, not from the commit it names. |
| 2026-09-25 | Applied in DevCoreApp. Changes 1–8 taken; 18 of the 24 shared files applied as ThreadIQ's diff with the namespace prefix swapped, the other six merged by hand. Deviations below. Build clean (Debug + ServiceMocks); 36 tests green, including the 19 added here. Manual verification steps 1–9 **not yet run**. Open question 1 remains open (hub decision pending). |

### Applied in DevCoreApp — deviations from the instructions

- **Unit of work, not `ApplicationDbContext`.** ThreadIQ's `UserProfileService` and `AccountService`
  inject the scoped context (`Db` / `dbContext`). DevCoreApp's services had already moved to the
  per-operation `RepositoryFactory.Create()` pattern, so every new data access here goes through
  `IQueryRepository`: organization resolution via `GetOrganizationsQuery().ByPublicIds(...)`, the
  assignment via `GetUserOrganizationQuery().CreateNew()` + `AddAsync`, and the batched Organization
  column via `GetUserOrganizationQuery().Select()`. Behaviour is the same. `CreateUserAsync` opens its
  one `repo` before the organization is resolved, so resolve-first ordering is kept.
- **Owner root organization: no tenant lookup.** There is no `Tenant` query on `IQueryRepository`,
  so `AssignRootOrganizationAsync` resolves the root as the parentless organization (lowest
  `SortOrder`), the doc's own fallback. `OrganizationDataSeeder` creates exactly one tenant whose
  `RootOrganizationId` is that organization, so the result is the same on a template install.
- **`IOrganizationService.GetCurrentAsync` added.** `NewUser` defaults its organization picker from
  it. ThreadIQ's `Core` has it but DevCoreApp's did not (it never came upstream). Ported as-is,
  outside this doc's scope list; the fan-out doc must carry it for any fork that also lacks it.
- **Already partly done here.** DevCoreApp's `CreateUserAsync` already called the no-password
  `CreateAsync` and already sent an email-confirmation token as an absolute link built from
  `HttpContext`. That builder is replaced by `IAccountLinkBuilder`; `IHttpContextAccessor` is no
  longer injected into `UserProfileService`.
- **Not taken:** ThreadIQ-local phone formatting (`FormatPhone` / `NormalizePhone`) and the
  browser time-zone field in `NewUser.razor`, both present in ThreadIQ's context lines. The
  `Users.razor` comment pointing at ThreadIQ's `docs/open-items.md` now points here instead.
- `registration.html` checked: it renders `<a href="{{Link}}">`, so the absolute URL is correct.

## Source change

The whole change is one commit on ThreadIQ `main` — `24f54d5`, *"#1124: New user experience"*,
30 files. To read it:

```bash
git -C <path-to-ThreadIQ> show --stat 24f54d5

# The shared surface only — every path that should influence DevCoreApp:
git -C <path-to-ThreadIQ> show 24f54d5 -- \
    src/Server/Admin/Services/Core \
    src/Server/Admin/WebService/Core \
    src/Server/Admin/WebService/Program.cs \
    src/Server/Admin/WebService/appsettings.json \
    src/Server/Database/Core/Data/Decorators/UserProfileDecorators.cs \
    src/Shared/Model/Core \
    mocks \
    tests
```

> Note the three explicit file paths in that list. `Program.cs` and `appsettings.json` sit at the
> project root and carry no `Core/` marker, and `UserProfileDecorators.cs` lives under
> `Database/Core` — already the shared root, so it is never doubled to `Core/Core`. A path filter of
> just `**/Core/**` silently misses all three, and the decorator change is load-bearing for the
> Organization column.
>
> That filtered command should report **24 files** — the commit's 30, less the six ThreadIQ-local
> ones named below. A different count means the path list was mistyped.

**Read the diff as a reference, not a patch.** Every file listed is shared surface, but the literal
text will not apply: ThreadIQ's namespaces carry a `DevInstance.ThreadIQ.` prefix where DevCoreApp
carries its own. The instructions in this document are authoritative; the diff is there to resolve
ambiguity about exact code shape.

Three paths in that commit are **ThreadIQ-local and must not be taken upstream**: `CLAUDE.md`,
`src/Server/Admin/WebService/CLAUDE.md`, and `docs/` (which includes this document — `docs/migration/`
is the delivery mechanism, not content to apply). Everything under `src/`, `mocks/` and `tests/` is
shared.

## Why

This is a defect in the template's own onboarding path, not in any product's domain code. It was
found in ThreadIQ only because email had not been configured on that deployment yet, which removed
the one mechanism that made the flow appear to half-work.

An administrator creating a user through **Admin → Users → New** produced an account that **could
never sign in by any route**, and which — had it signed in — would have read every organization in
the database while being unable to write a single row. Of the ten defects below, three are each
individually sufficient to block sign-in and a fourth made the emailed link useless even where SMTP
was working correctly. The last three are not fatal but make the users list actively misleading, and
they are included here because repairing the flow without them leaves every recovered user still
reading `INITIATED` behind a grey badge.

None of this is reachable by the self-registration path, which is why it survived: `RegisterAsync`
sends a *confirmation* link and lets the user pick their own password, so it is the administrator
path alone that is broken.

## Current state

Behaviour in DevCoreApp `Core` as of this writing. Rows 1–7 are each independently fatal to the
flow; rows 8–10 are display defects in the users list.

| # | Surface | Behaviour | Consequence |
|---|---|---|---|
| 1 | `UserProfileService.CreateUserAsync` | Creates the `ApplicationUser` with `IdGenerator.New()` as a password, then discards it | The user has a password nobody knows. `ConfirmEmailAsync` gates its set-password step on `HasPasswordAsync`, which is now `true`, so the built-in recovery path refuses to offer the form |
| 2 | `Database.Core.ConfigurationExtension` | `SignIn.RequireConfirmedAccount = true` | Nothing in the administrator path ever confirms the address, so `SignInManager` returns `IsNotAllowed` regardless of password |
| 3 | `AccountService.SendPasswordResetLinkAsync` | Requires `IsEmailConfirmedAsync` before sending, and always reports success to prevent user enumeration | "Forgot password" is a **silent** dead end for these users — no mail, no error, not even a failed `EmailLog` row |
| 4 | `UserProfileService.SendRegistrationEmailAsync` | Passes the raw `GeneratePasswordResetTokenAsync` value as `{{Link}}` into `registration.html`, which renders `<a href="{{Link}}">` | The invitation renders as `href="CfDJ8Nx…"` — a relative navigation to a nonexistent path. Also hardcodes `From = noreply@example.com` rather than reading `EmailConfiguration`, which most providers reject |
| 5 | `UserProfileService.CreateUserAsync` | Writes no `UserOrganizations` row | `OrganizationContextResolver` returns an empty context. The org query filter is **fail-open** on an empty visible set, so the user reads *every* organization; `OrganizationStampInterceptor` throws on every scoped insert. Read everything, write nothing |
| 6 | `AccountService.SetupOwnerAsync` | Same omission for the very first account | The owner is in the same state. In practice every deployment has had this repaired by hand through Edit User → Organizations without anyone recording why |
| 7 | `UI.Pages.Account.ForgotPassword` | Builds `ToAbsoluteUri("Account/ResetPassword")`; the page is routed `/account/reset-password` | Routing ignores case but not the missing hyphen. **Every password-reset email in the template points at a 404** — this one is not specific to administrator-created users |
| 8 | `UserProfile.Status` | Written in exactly two places (`INITIATED` on admin create, `LIVE` on owner setup) and **read in none** | Every invited user reads `INITIATED` forever, however long ago they confirmed and signed in. `SUSPENDED` and `UNKNOWN` are unreachable |
| 9 | `UI.Pages.Admin.Users` status badges | Compare against `"Active"` and `"Suspended"`; the decorator emits `profile.Status.ToString()` — `"LIVE"`, `"SUSPENDED"` | Wrong word *and* wrong case, so **both branches are dead** and every user renders the grey fallback badge |
| 10 | `UI.Pages.Admin.Users` advanced filters | Build `status:` / `field:` / `days:` tokens into the search string; `CoreUserProfilesQuery.Search` substring-matches names, email and phone only, and nothing parses the tokens | Choosing any of the three **empties the grid** — it searches for the literal text `status:Active` in people's names |

There is also no administrator-facing remedy for any of it: no resend, no set-password, no way to
see which half of the requirement is outstanding, and no way to obtain the invitation link when
outbound email does not work.

## Proposed change

Eight changes. **Numbers 1–3 are interdependent** — applying any one of them alone leaves the flow
broken, so they should land together. Numbers 4–8 are independently useful; 7 and 8 are presentation
only, but they are what make the admin list trustworthy after 1–3.

### 1. Create the user with no password

In `CreateUserAsync`, call `UserManager.CreateAsync(user)` — the no-password overload. Delete the
`IdGenerator.New()` placeholder.

This is what makes the *existing, already-written* `ConfirmEmailAsync` → `needsPassword` →
`ConfirmEmail.razor` set-password form engage; that code is present in the template today and is
simply unreachable. `AccountService.SetPasswordAsync` uses `AddPasswordAsync`, which fails outright
when a password already exists, so the placeholder was blocking it twice over.

While in this method, make a failed `AddToRoleAsync` **throw** rather than log. A roleless user has
no permissions and no route to acquiring any, and the caller was being told the create succeeded.

### 2. Assign exactly one organization, in the same call

Resolve the organization **before** creating anything, so the method fails without leaving a
half-built account:

```csharp
private async Task<Guid> ResolveNewUserOrganizationIdAsync(string? organizationPublicId)
{
    if (!string.IsNullOrWhiteSpace(organizationPublicId))
    {
        var organization = await Db.Organizations
            .FirstOrDefaultAsync(o => o.PublicId == organizationPublicId);
        if (organization == null)
            throw new RecordNotFoundException($"Organization '{organizationPublicId}' not found.");
        return organization.Id;
    }

    return OperationContext.PrimaryOrganizationId
        ?? throw new BusinessRuleException(
            "Cannot create a user: no organization could be resolved for this operation. …");
}
```

Then write the assignment, mirroring exactly what `SetUserOrganizationsAsync` already does —
including the `ApplicationUser.PrimaryOrganizationId` mirror column and the resolver cache
invalidation, without which the assignment is invisible for up to 30 minutes:

```csharp
Db.UserOrganizations.Add(new UserOrganization
{
    Id = Guid.NewGuid(),
    UserId = user.Id,
    OrganizationId = organizationId,
    Scope = OrganizationAccessScope.Self,
    IsPrimary = true
});

user.PrimaryOrganizationId = organizationId;
await UserManager.UpdateAsync(user);
await Db.SaveChangesAsync();
OrgResolver.InvalidateCache(user.Id);
```

Add an overload so the caller can name the organization, keeping the old signature for the import
handler:

```csharp
Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role);
Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role, string? organizationPublicId);
```

**`SetupOwnerAsync` needs the same treatment**, resolving the seeded root organization from
`Tenants.RootOrganizationId` (falling back to the `ParentId == null` organization). Use scope
**`WithChildren`**, not `Self`: the owner sits at the top of the tree and every organization created
later hangs beneath the root, so `Self` would hide each new one until assigned by hand.

> **Watch the ordering.** Resolving the organization first is not cosmetic. If it throws after
> `UserManager.CreateAsync`, the `ApplicationUser` is already committed — Identity does not enlist
> in the surrounding unit of work — and the email address is then permanently taken by an account
> with no profile.

### 3. Build a real link, in exactly one place

Introduce `IAccountLinkBuilder` in `Server.Admin.Services.Core.Account`, modelled on the existing
`IMailRedirectUriBuilder` (config key first, `IHttpContextAccessor` fallback). It owns the account
route constants:

```csharp
public static class AccountRoutes
{
    public const string ConfirmEmail  = "/account/confirm-email";
    public const string ResetPassword = "/account/reset-password";
    public const string Login         = "/account/login";
}
```

`TryBuildBase()` returns `string?` rather than throwing — bulk user import runs on the background
queue with no `HttpContext` at all, and that caller must degrade rather than fail.

Config key `App:BaseUrl`: optional inside a request, **required** for links built by background
work, and required behind a TLS-terminating proxy where `Request.Scheme` reports `http` (the same
reason `Mail:RedirectBaseUrl` exists).

`SendRegistrationEmailAsync` then becomes `SendInvitationEmailAsync`, which:
- generates an **email-confirmation** token (not a password-reset token), Base64Url-encodes it as
  `RegisterAsync` already does, and builds `{origin}/account/confirm-email?userId=…&code=…`;
- reads `From` from `EmailConfiguration:FromEmail` / `:FromName`;
- returns `false` **without queueing** when no origin can be resolved. An invitation whose only call
  to action is a broken link is worse than no invitation, and the administrator has the copyable
  link either way (change 4).

Register as `AddScoped<IAccountLinkBuilder, AccountLinkBuilder>()` next to the mail redirect builder.

Fix `ForgotPassword.razor.cs` to `ToAbsoluteUri("account/reset-password")` — defect 7. This is the
reason the routes are constants now; every future emailed link should be built through the builder
rather than by hand.

`registration.html` needs **no change**, but confirm it in the receiving repo: `{{Link}}` changes
meaning from "an opaque token" to "an absolute URL", so any fork that worked around the old
behaviour by rendering the token as text will now show a URL twice.

### 4. Give the administrator a remedy — Edit User → Access

A new tab on `EditUser`, backed by three service methods and one DTO
(`Shared.Model.Core.UserAdmin.UserAccessStateItem`):

```csharp
Task<ServiceActionResult<UserAccessStateItem>> GetUserAccessStateAsync(string userId);
Task<ServiceActionResult<bool>>                ResendInvitationAsync(string userId);
Task<ServiceActionResult<bool>>                SetUserPasswordAsync(string userId, string password);
```

The DTO carries `EmailConfirmed`, `HasPassword`, `InvitationLink` and `CanSignIn => EmailConfirmed
&& HasPassword`. **Show both flags separately** — they fail independently, and an administrator
otherwise cannot tell which half is outstanding.

The tab renders the two status badges, the invitation link in a read-only field with a copy button
(`copyTextToClipboard`, already in `Scripts/app.ts`), a resend button, and a set-password form.
The link section is hidden once `CanSignIn`, and shows an explicit "configure `App:BaseUrl`" warning
when the origin cannot be resolved.

**`SetUserPasswordAsync` must also confirm the email address.** With `RequireConfirmedAccount`
enabled, setting a password alone still leaves the user unable to sign in, so a set-password button
that did not confirm would appear to work and change nothing. An administrator who has just handed
over a password through a trusted channel has vouched for the address at least as firmly as a
confirmation mail would. Implement it via `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`
rather than `AddPasswordAsync`, so the one method serves both an invited user with no password and
an existing user who has forgotten theirs.

Add an organization picker to `NewUser`, defaulting to the creating administrator's own
(`IOrganizationService.GetCurrentAsync`). Note that `GetTreeAsync` returns a **flat list already
ordered by materialized `Path`** despite the name — indent by `Level`, do not try to walk children.

### 5. Scope the import commit to its session's organization

`BackgroundTaskWorker.ProcessTaskAsync` calls `operationContext.Reset()` and never restores the
task's organization, so background work runs org-less. That was survivable while nothing in the
background inserted org-scoped rows through the interceptor; with change 2 in place, **every row of
a bulk user import throws**.

In `ImportDataTaskHandler.HandleAsync`, after loading the session and before the commit:

```csharp
var operationContext = scopedProvider.GetRequiredService<BackgroundOperationContext>();
if (session.OrganizationId != Guid.Empty)
{
    operationContext.PrimaryOrganizationId = session.OrganizationId;
    operationContext.SetVisibleOrganizationIds([session.OrganizationId]);
}
```

> **Deliberately scoped to this one handler.** The general fix is for `ProcessTaskAsync` to
> establish the context from `task.OrganizationId` for *every* handler, which would also close the
> fail-open read hole across all background work. ThreadIQ did not take that route here because it
> silently narrows read visibility for mail sync, calendar sync and reassignment sweeps, and that
> deserves its own change with its own regression pass. **DevCoreApp is the right place to decide
> this** — see Open questions.

### 6. Report `NeedsPassword` from `AlreadyConfirmedResult`

`ConfirmEmailResult.AlreadyConfirmedResult(userId)` hardcodes `NeedsPassword = false`. Harmless
while nothing routed administrator-created users to that page; now it is the primary onboarding
path, and an invitee who opens the link twice before choosing a password is told "you can now log
in" while holding no password.

```csharp
return ConfirmEmailResult.AlreadyConfirmedResult(
    userId, needsPassword: !await userManager.HasPasswordAsync(user));
```

In `ConfirmEmail.razor`, move the `passwordSet` branch **above** `emailConfirmed && !needsPassword`.
It currently sits below and is unreachable: setting a password makes both conditions true, so the
earlier branch always wins and the user is shown "Email Confirmed" instead of "Account Ready".

### 7. Advance `Status` to `LIVE`, and stop rendering the enum name

**Advance the status.** Add a pure predicate beside `AccountLinkBuilder` so the rule lives in one
place and is unit-testable without a database:

```csharp
public static bool ShouldActivate(UserStatus current, bool emailConfirmed, bool hasPassword)
{
    if (!emailConfirmed || !hasPassword) return false;
    return current == UserStatus.INITIATED || current == UserStatus.UNKNOWN;
}
```

Both conditions are required because both gate sign-in independently — that is the only thing
`LIVE` has ever been intended to mean. **`SUSPENDED` is deliberately not promoted**: nothing writes
it today, but if suspension is ever built, confirming an address or resetting a password must not
quietly lift it.

Call it from the three points where an account can become usable:
`AccountService.ConfirmEmailAsync` (only when the user already had a password — otherwise
`SetPasswordAsync` handles it a step later), `AccountService.SetPasswordAsync`, and
`UserProfileService.SetUserPasswordAsync` from change 4. Guard against a missing profile: self
registration creates none (see Open questions).

**Add display labels.** The DTO keeps the enum name as `Status` — it is the machine value, and
comparisons, serialization and saved grid profiles depend on it being stable — and gains
`StatusLabel`, resolved by a new `Shared.Model.Core.UserAdmin.UserStatusLabels`:

| Machine value | Label |
|---|---|
| `UNKNOWN` | Unknown |
| `INITIATED` | **Invited** |
| `LIVE` | **Active** |
| `SUSPENDED` | Suspended |

`INITIATED → "Invited"` rather than "Initiated": it is what an administrator actually wants to read
off the list, and it matches the invitation flow this document introduces.

Two properties worth keeping when you port it. Lookup is **case-insensitive**, so a mock or an older
row spelling `"Initiated"` still resolves. An **unrecognised value is returned unchanged** rather
than blanked — losing it would hide exactly the case worth noticing.

`UserStatusLabels` lives in `Shared.Model`, not beside the enum, because the WASM client cannot
reference the Database project. It is **the single localization seam**: when resources arrive,
`For()` becomes an `IStringLocalizer` lookup on the `ResourceKey()` values (already in the
conventional `UserStatus_Live` shape) with the table above as fallback, and no caller changes
because no caller hardcodes a label. DevCoreApp registers `AddLocalization()` today but has no
`.resx` and no `IStringLocalizer` consumer, so this deliberately stops short of building that
infrastructure.

**Fix the badges** — colour keys off the machine value, text always comes from `StatusLabel`:

```razor
<span class="badge @StatusBadgeClass(user.Status)">@user.StatusLabel</span>
```

**Remove the three advanced search controls** (defect 10). They cannot be made to work without
parsing support that does not exist, and a filter that silently returns nothing is worse than no
filter. See Open questions for building it properly.

### 8. An optional Organization column on the users list

Now that every user has exactly one organization (change 2), the admin list should be able to show
it — but most single-organization installations do not want the column, so it ships **present and
switched off**, discoverable through grid settings like `Middle Name` already is:

```csharp
new() { Label = "Organization", Field = "organization", ValueSelector = u => u.OrganizationName,
        IsSortable = false, IsVisible = false, Width = "14%" },
```

`IsSortable = false` is deliberate: the name lives on `Organizations` via `UserOrganizations`, and
`CoreUserProfilesQuery.SortBy` only orders columns on `UserProfiles` itself.

`UserProfileItem` gains `OrganizationName`, and `UserProfileDecorators.ToView` gains a fourth
optional parameter to carry it — the same shape as the existing `roles` parameter, and for the same
reason: the value comes from outside the profile row. All existing call sites keep working.

**Load it batched.** `GetListAsync` already pays an N+1 to Identity for roles; a hidden column must
not add a second query per row:

```csharp
var rows = await Db.UserOrganizations
    .Where(uo => appUserIds.Contains(uo.UserId) && uo.IsPrimary && uo.Organization != null)
    .Select(uo => new { uo.UserId, uo.Organization!.Name })
    .ToListAsync();

// GroupBy, not ToDictionaryAsync: "exactly one primary" is enforced by
// SetUserOrganizationsAsync on write, not by a database constraint, so a legacy
// double-primary row pair must not throw here.
return rows.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.First().Name);
```

Only the list read populates it; single-user reads leave it empty. An empty value also legitimately
means *no assignment* — which, before change 2, is exactly the state worth spotting.

> **Not a new disclosure.** `UserProfile` does not implement `IOrganizationScoped`, so the users
> list is already global across organizations for anyone holding `Owner`/`Admin`. This column names
> organizations that were already reachable through Edit User → Organizations. If a fork has
> narrowed that list, check this column against the same rule.

## Affected shared surface

| Sync key | Change |
|---|---|
| `Server.Admin.Services.Core.Account.{IAccountLinkBuilder,AccountLinkBuilder,AccountRoutes}` | **New file.** |
| `Server.Admin.Services.Core.UserAdmin.UserProfileService` | No-password create; org assignment; invitation link; access-state, resend and set-password methods |
| `Server.Admin.Services.Core.UserAdmin.IUserProfileService` | `CreateUserAsync` overload + three new methods |
| `Server.Admin.Services.Core.AccountService` | `AssignRootOrganizationAsync` in owner setup; `NeedsPassword` on already-confirmed; activate to `LIVE` |
| `Server.Admin.Services.Core.Account.UserActivation` | **New file.** The `ShouldActivate` predicate |
| `Server.Admin.Services.Core.Background.Tasks.Handlers.ImportDataTaskHandler` | Establish the session's organization context |
| `Shared.Model.Core.UserAdmin.UserAccessStateItem` | **New file.** |
| `Shared.Model.Core.UserAdmin.UserStatusLabels` | **New file.** Display labels + localization seam |
| `Shared.Model.Core.UserProfileItem` | `StatusLabel` computed property; `OrganizationName`; `Status` documented as the machine value |
| `Server.Admin.WebService.Core.UI.Pages.Admin.Users` | Status badges; the three dead search controls removed; optional Organization column |
| `Server.Database.Core.Data.Decorators.UserProfileDecorators` | `ToView` gains an optional `organizationName` |
| `Shared.Model.Core.Account.ConfirmEmailResult` | `AlreadyConfirmedResult` takes `needsPassword` |
| `Server.Admin.WebService.Core.UI.Pages.Admin.NewUser` | Organization picker |
| `Server.Admin.WebService.Core.UI.Pages.Admin.EditUser` | Access tab |
| `Server.Admin.WebService.Core.UI.Pages.Account.ConfirmEmail` | Branch order |
| `Server.Admin.WebService.Core.UI.Pages.Account.ForgotPassword` | Reset-link route |
| `Server.Admin.WebService.Program` | One `AddScoped` line (root file, unmarked, still shared) |
| `appsettings.json` | `App:BaseUrl` (root file, unmarked, still shared) |
| `Server.Admin.Services.Mocks.Core.UserAdmin.UserProfileServiceMock` | Implement the four new interface members |

**Fence check.** Verified: ThreadIQ has no `#region project-specific` fence in any file listed
above. Its four fences sit in `UI.Pages.Admin.Organizations.{OrganizationIndex,OrganizationDetail}`
and in `Services.Core.Authentication.PermissionClaimsTransformation` (API-key scope intersection) —
adjacent surface, but nothing this change touches, and the claims fence does not affect the
organization claims these changes depend on. A receiving fork should still grep before applying;
`UserProfileService` and `AccountService` are plausible places for a fork to have deviated.

**`appsettings.json` note.** ThreadIQ documents `App:BaseUrl` with a `//` comment. Both the .NET
configuration provider and PowerShell's `ConvertFrom-Json` accept it (verified — ThreadIQ's IIS
deploy script rewrites this file through `ConvertFrom-Json`/`ConvertTo-Json`), but confirm against
your own deployment tooling before copying the comment.

## Verification

Build and test:

```bash
dotnet build
dotnet test
dotnet build mocks/…/ServicesMocks.csproj -c ServiceMocks   # the mock must satisfy the widened interface
```

ThreadIQ: 0 errors, **491 tests green** (19 added), mocks configuration builds. The two pure
helpers introduced here are covered — `UserActivationTests` (7) pins the activation rule including
"never lifts a suspension", and `UserStatusLabelsTests` (12) pins the labels, the case-insensitive
lookup, the unrecognised-value passthrough and the resource keys. The services around them read
through `IQueryRepository` and are not unit-testable without more machinery (see the testing notes
in the root `CLAUDE.md`), so the rest of **verification is manual**, and the sequence matters:

1. **Fresh database, first run.** Complete owner setup, then confirm a `UserOrganizations` row
   exists for the owner with `IsPrimary = true` and `Scope = WithChildren`. Before this change there
   is none.
2. **Create a user with email deliberately unconfigured.** Expect: the account is created; the
   `EmailLog` row records the failure; the Access tab shows *Email not confirmed* + *No password*.
3. **Copy the invitation link, open it in a private window.** Expect the set-password form — not
   "you can now log in". Set a password, then sign in.
4. **Open the same link a second time before setting a password** (defect 6). Expect the
   set-password form again, not a dead end.
5. **Confirm the new user's scope.** They must see only their own organization's data. Before this
   change an unassigned user saw every organization's rows.
6. **Bulk-import users from CSV** (change 5). Every row must land in the session's organization. This
   is the regression most likely to be missed.
7. **Forgot password on a confirmed account** (defect 7). The emailed link must resolve, not 404.
8. **Check the status column after step 3.** The invited user must read **Active**, not `INITIATED`
   and not a grey badge. Before this change they read `INITIATED` forever.
9. **Switch on the Organization column** in grid settings. It must default to off, survive a reload
   (it is persisted in the grid profile), and show the organization assigned in step 5 — with no
   extra query per row.

## Open questions

1. **Should `BackgroundTaskWorker.ProcessTaskAsync` establish the organization context for every
   handler?** ThreadIQ fixed only `ImportDataTaskHandler` to keep the blast radius proportionate,
   but the general defect is real and belongs to the template: background work currently runs
   fail-open across every organization, which the root `CLAUDE.md` already names as a silent
   cross-org data leak. DevCoreApp owns this call. If taken, ThreadIQ's local `ImportDataTaskHandler`
   change should be reverted in favour of it.
2. **Should self-registration be closed or completed?** `RegisterAsync` creates an `ApplicationUser`
   with no `UserProfile`, no role and no organization, so `IOperationContext.UserId` resolves to null
   for such an account. `/account/register` is routable in the template even though nothing links to
   it. This is a product decision, not a repair, so it is out of scope here — ThreadIQ has logged it
   locally in `docs/open-items.md`.
3. **Should `UserStatus` become an access gate?** Change 7 makes it *descriptive* and correct, but
   nothing gates on it. `SUSPENDED` remains unwritten and unread, so there is still no way to
   suspend an account. Making it mean something needs a decision about the login path (refuse at
   sign-in? revoke live sessions?), an admin suspend/reinstate action, and whether `UNKNOWN` should
   ever be legal. `UserActivation.ShouldActivate` already refuses to promote `SUSPENDED`, so the
   policy half is in place for whoever builds the feature. Related: `DeleteUserAsync` is a hard
   delete, and "disable" is probably what most administrators actually reach for.
4. **A real users-grid filter.** `CoreUserProfilesQuery.Search` substring-matches names, email and
   phone only. Change 7 removes the broken token-based controls rather than repairing them, but
   "who has not accepted their invitation" becomes a natural question the moment `Status` is
   trustworthy. Doing it properly means `ByStatus` on `IUserProfilesQuery` and a real filter
   argument on `GetListAsync`, both shared surface — which is why it is a hub decision rather than
   something ThreadIQ should widen unilaterally.
