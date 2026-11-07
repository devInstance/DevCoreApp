---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Server.Admin.Services.Core.Authentication
  - Server.Admin.Services.Core.AccountService
  - Server.Admin.Services.Core.Account
  - Server.Admin.Services.Core.UserAdmin
  - Server.Admin.Services.Core.Organizations
  - Server.Admin.Services.Core.Seeding
  - Server.Admin.WebService.Core.Identity
  - Server.Admin.WebService.Core.Controllers.AuthController
  - Server.Admin.WebService.Core.UI.Pages.Account
  - Server.Admin.WebService.Core.UI.Pages.Admin
  - Server.Admin.WebService.Program
  - Server.Database.Core.Models
  - Server.Database.Core.Data
  - Server.Database.Core.ApplicationDbContext
  - Shared.Model.Core.Authentication
  - Shared.Model.Core.Account
  - Shared.Model.Core.UserAdmin
  - Shared.Model.Core.Organizations
status: pending
change: "TBD - planning only; no implementation commit exists"
related:
  - docs/organization-authentication-plan.md
  - docs/migration/out/2026-08-28-admin-created-user-onboarding.md
---

# Entra login and organization-controlled authentication methods

## Why

Support Microsoft Entra ID login/registration alongside native email/password, with organization
admins enabling/disabling methods. Default every method preference to enabled. Support later Google
and optional Facebook adapters through the same policy and linking pipeline. This is reusable Core
functionality for DevCoreApp and downstream products.

**Planning handoff only:** nothing described here has been implemented in ThreadIQ yet. The full
design is [the ThreadIQ plan](../../organization-authentication-plan.md). Keep this note pending;
do not mark it applied merely because the document has been copied. This turn authored the outbound
note in ThreadIQ only; it did not modify or deliver files into DevCoreApp.

## Existing implementation check — 2026-09-12

Inspected `C:/Projects/devInstance/DevCoreApp`, HEAD `66775b8`, including working-tree files.
The checkout has unrelated local changes; preserve them. ThreadIQ baseline was `951a302`.
This is a local source review, not a statement about remote branches or deployed features.

| Shared surface | DevCoreApp current state | Required work |
|---|---|---|
| `Server.Admin.WebService.Program` | `Smart` routes cookies/JWT/API keys; Identity external cookie is registered | Add named Entra OIDC handler without changing default routing |
| `Server.Admin.WebService.Core.Identity.IdentityComponentsEndpointRouteBuilderExtensions` | Additional account endpoint is logout | Add protected challenge/complete/link endpoints |
| `Server.Admin.Services.Core.AccountService` | Password registration/sign-in; registration does not create profile/membership | Common policy checks and complete native/external onboarding |
| `Server.Admin.Services.Core.Authentication.JwtAuthService` | Password-issued app JWTs and rotating refresh tokens | Method metadata, policy-aware refresh, external handoff exchange |
| `Server.Database.Core.Models.ApplicationUser` / Identity schema | Local Identity accounts and login-link table foundation | Reuse `AspNetUserLogins`; do not duplicate account storage |
| `Server.Database.Core.Models.UserOrganization` | Membership, scope, primary flag; no authentication-admin grant | Explicit org-specific management authority |
| `Server.Database.Core.Models.Setting` | User/org/tenant/system value resolution | Use separate typed authentication policy so users cannot override it |
| Provider implementation | No Entra/Google/Facebook user-login handler or linking flow found | Feature is not already implemented in inspected source |

The earlier `2026-08-28-admin-created-user-onboarding.md` exists in DevCoreApp's `in/` with
`status: pending`. Reconcile/apply its shared account-link, activation, access-state, and organization
assignment work before integrating this feature. Extend its password-only activation rule to accept
linked external credentials. Merely finding the inbound file does not establish implementation.

## Proposed change

1. Add stable method keys (`password`, `entra`, `google`, `facebook`) and a method catalog separating
   implemented/configured availability from org-enabled preference. Every preference defaults true;
   future or unconfigured providers are not actionable and cannot prevent lockout.
2. Add typed `OrganizationAuthenticationPolicy`, unique per org, with default-true preferences,
   accepted Entra tenant IDs, policy concurrency/version, and audit fields. Seed/backfill existing
   orgs, initialize new orgs, and reject unknown/malformed contexts. No parent inheritance in v1.
3. Add `CanManageAuthentication` to explicit user-org assignments and
   `Organizations.Authentication.Manage` permission. Require both in the service. Platform Owner
   may manage/recover explicitly; visibility or general membership is not admin authority. Prevent
   last-admin removal and concurrent changes leaving no available authentication method.
4. Add `IOrganizationAuthenticationPolicyService`, `IAuthenticationMethodCatalog`, and common
   sign-in eligibility/provisioning services. Follow per-operation repository factory, typed
   query/DTO, public-ID, `ServiceActionResult<T>`, audit exclusion, and LogScope conventions.
5. Introduce org-code/link discovery and protected authentication transaction state. Treat the
   selected org as a requested destination, then independently validate membership/admission.
   Registration into an existing organization requires an admin invitation for every method
   (confirmed by the user on 2026-09-12). Enforce invitation validity server-side for native and
   external registration; org links and matching email domains do not authorize membership.
   Open joining and new-customer organization provisioning are outside this feature.
6. Add Entra code-flow OIDC with PKCE into the Identity external cookie, then map to a local user.
   Validate issuer/signature/audience/nonce and org tenant acceptance; use a supported multitenant
   issuer validator. Keep `Smart` and local roles. Request `openid profile email` only. Separate
   protocol callback `/signin-entra` from application completion `/account/external/complete`.
7. Use validated `(tid, oid)` for Entra links in `AspNetUserLogins`. Never auto-link matching emails.
   Require existing-account proof or a bound single-use invitation; collect/verify contact email,
   create profile/membership/link consistently, and support passwordless activation. Enforce
   credential uniqueness, transaction safety, and proof-required unlinking/password setup.
8. Enforce policy on native SSR/API login, external completion, registration, credential management,
   refresh, JWT requests, and live Blazor service operations. Filter each visible org against the
   actual session method/tenant; primary org must remain eligible. Empty eligibility denies access
   before today's fail-open organization filter. Preserve explicit system-job behavior and scope
   jobs initiated by users. Do not rely on the existing 30-minute org cache for security policy.
9. Add actual method/external identity metadata to local cookies, JWTs, refresh records, and safe
   login history. Copy on refresh rotation; recheck policy at each protected operation. Compose
   cookie validation with security-stamp validation. Treat pre-cutover legacy sessions as password
   only during a bounded transition; missing metadata on new sessions is invalid.
10. Add server-side mobile handoff/exchange: short-lived hashed code, atomic single use, originating
    browser PKCE binding, allowlisted client destinations, fresh policy check. Exchange via POST for
    local JWTs. Provider/app tokens and refresh tokens must never appear in redirect URLs.
11. Add admin settings and account-link screens; reject last-available-method removal, require recent
    proof through a retained method when disabling the admin's method, and show affected members.
    Record policy/admin changes without credentials. Password/Entra policy does not revoke API keys.

**Related prerequisite:** inspect `AccountService.SetPasswordAsync` and the already-confirmed branch
of `ConfirmEmailAsync`. They must require proof of account control at the mutation boundary;
knowing a user ID or visiting an already-confirmed account URL is insufficient. External passwordless
users must not become vulnerable through the inherited password-setup path.

## Affected shared surface and receiving-repo differences

In addition to the front-matter areas, implementation should include:

- Database query interfaces/implementations and repository registration for policies, invitation
  consumption, and handoff transactions; model configuration; refresh-token and login-history
  metadata; organization seeding/create paths; management-grant assignment DTOs.
- `Core.Authentication.ConfigurationExtensions`, `ApplicationSignManager`,
  `PermissionClaimsTransformation`, `OrganizationContextResolver`, and session validators.
- `Core.AccountService`, account activation/link helpers, and `Core.UserAdmin.UserProfileService`
  plus corresponding mocks/access-state DTOs, permission definitions and seeder.
- Account login/register/confirm/recovery/link components; admin organization authentication UI;
  `AuthController` and new external endpoint group; Identity revalidation; host DI/configuration
  and matching ASP.NET Core 10 OIDC package reference. Both hosts target `net10.0`.
- Relational database migrations/snapshots and auth/organization/endpoint tests. Generate each
  repository's migrations locally following its rules; never copy a fork snapshot wholesale.

ThreadIQ has `Core.UI.Pages.Admin.Organizations.OrganizationIndex/OrganizationDetail`; DevCoreApp
has `Core.UI.Pages.Admin.OrganizationTreePage`. Integrate controls into the target's existing UI.
Do not replace that page structure wholesale.

ThreadIQ's `AddAppIdentity` takes configuration and includes persistent Data Protection/cookie
configuration; DevCoreApp's inspected overload takes no configuration. Merge the needed options
and DI deliberately. Fix `HttpOnly=false` for application cookies as part of session work and persist
Data Protection keys outside deployment folders.

Preserve any `#region project-specific` fences, especially ThreadIQ's API-key permission intersection.
DevCoreApp has a different client project/layout; carry shared server endpoints and DTOs first, then
adapt its client based on actual hosting/token behavior. ThreadIQ's `/mobile`, `AppNavigation`, and
offline token storage are product-specific and must not be copied as template requirements.

## Source change and delivery

No implementation diff exists yet. After the implementation is committed, replace `change` with
its source SHA and update this note to match the final behavior and actual touched sync keys.
Then review, for example:

```powershell
git -C <ThreadIQ-path> show <implementation-sha> -- src/Server/Admin/Services/Core src/Server/Admin/WebService/Core src/Server/Database/Core src/Shared/Model/Core
```

Review host `Program.cs`, project package references, and configuration separately. A diff is a
reference, not an applicable patch: product namespace prefixes differ. Exclude `App` code (including
`Database/Core/App`), Graph mailbox/calendar adapters and secrets, ThreadIQ mobile UI/hosting,
product styles, and unrelated local guidance. This instruction document is the intended documentation
handoff; do not broadly sync ThreadIQ's other docs into the template.

Deliver through the normal hub-and-spoke workflow into DevCoreApp `docs/migration/in/`. When built
and verified there, mark applied and move to `in/applied/`; DevCoreApp then authors outbound notes
for its other forks. Keep this planning note pending until that implementation exists.

## Verification

- Build `src/Server/Admin/WebService/DevCoreApp.Admin.WebService.csproj` and affected client projects.
  Run focused auth/organization tests under `tests/Server/WebService` and `tests/Server/Database/Core`.
- Prove enabled defaults, unavailable-provider fallback rules, target-org authorization and policy
  concurrency, Entra-only registration/login, secure linking, and passwordless profile activation.
- Prove native and external registration reject missing, expired, revoked, consumed, and wrong-org
  invitations without creating membership.
- Prove policy enforcement across SSR/API/refresh/live circuits, legacy password sessions,
  multiple orgs and descendants, empty visibility, direct-ID access, and user-initiated jobs.
- Exercise malformed/replayed callback state, wrong issuer/tenant/audience, duplicate links,
  email collisions, proof-required password setup, handoff replay/client binding, and partial
  onboarding failures. Use a relational DB for uniqueness/atomicity/concurrency claims.
- Verify native login/refresh, Owner bootstrap/recovery, API-key scopes, and disabled-account
  behavior remain intentional. Run a real nonproduction Entra round trip and recovery drill.
- Review PostgreSQL backfill/migration and SQL Server support before claiming both providers work.
  This plan does not authorize scaffolding EF migrations contrary to local repository rules.

## Open questions

Confirm workforce-only account audience and shared Entra app registration, exact-org policies
without inheritance, and initial org-admin grant assignments. Invitation-required registration into
existing organizations is settled. The remaining source-plan defaults are proposed; these are not
existing product capabilities.
