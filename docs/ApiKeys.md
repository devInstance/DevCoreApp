# API Keys

## Overview

The API key feature provides non-interactive authentication for integrations that need to call the application without using the browser login flow.

In the current starter implementation, API keys support:

- one-time key generation with hashed storage
- admin-side key creation, listing, and revocation
- optional expiration dates
- stable permission snapshots stored on the key
- optional organization association in the data model and admin UI
- authentication through the `X-Api-Key` header
- last-used tracking

This is an outbound client credential feature for calling this application. It is not an OAuth client-credentials server and it does not currently provide key rotation, key editing, or partial secret reveal after creation.

## Why This Feature Exists

The starter includes API keys so new projects can support:

- server-to-server integrations
- automation scripts
- internal tooling
- background sync jobs
- controlled access without sharing a user password

The goal is to give teams a simple, database-backed machine authentication model that is good enough for a starter project and can be extended later if needed.

## High-Level Design

The API key stack is split across these parts:

| Area | Responsibility | Main files |
|---|---|---|
| Data model | Stores key metadata, hash, scope snapshot, and revocation state | `src/Server/Database/Core/Models/ApiKey.cs` |
| EF configuration | Configures indexes, JSON storage, and relationships | `src/Server/Database/Core/ApplicationDbContext.cs` |
| Query layer | Search, sort, paging, and lookup by key hash or creator | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreApiKeyQuery.cs` |
| Decorators | Maps DB entity to shared admin/service model | `src/Server/Database/Core/Data/Decorators/ApiKeyDecorators.cs` |
| Shared DTOs | Validation rules and create-result contract | `src/Shared/Model/ApiKeys/ApiKeyItem.cs` |
| Admin service | Creates, lists, and revokes keys | `src/Server/Admin/Services/ApiKeys/ApiKeyAdminService.cs` |
| Auth handler | Authenticates requests from `X-Api-Key` | `src/Server/Admin/WebService/Identity/ApiKeyAuthenticationHandler.cs` |
| Permission transformation | Converts the key’s stored scopes into permission claims | `src/Server/Admin/Services/Authentication/PermissionClaimsTransformation.cs` |
| Admin UI | Lets admins manage keys | `src/Server/Admin/WebService/UI/Pages/Admin/ApiKeysPage.razor` |
| Seeder | Backfills legacy empty-scope keys to stable snapshots | `src/Server/Admin/Services/Seeding/ApiKeyDataSeeder.cs` |

The runtime flow is:

1. A client sends `X-Api-Key: <plain-text-key>`.
2. The smart auth selector routes the request to the API key scheme.
3. `ApiKeyAuthenticationHandler` hashes the supplied key and looks up the matching record.
4. The handler rejects revoked, expired, inactive, or owner-inactive keys.
5. The authenticated principal is created for the owning `ApplicationUser`.
6. `PermissionClaimsTransformation` loads the key’s stored scopes and emits `Permission` claims from that list.
7. Authorization policies continue to work through the normal permission system.

## Data Model

The persisted entity is `ApiKey`.

```csharp
public class ApiKey : DatabaseEntityObject
{
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public List<string>? Scopes { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Organization? Organization { get; set; }
}
```

### Field meanings

| Field | Type | Meaning |
|---|---|---|
| `Name` | `string` | Human-readable label for admins. |
| `KeyHash` | `string` | SHA-256 hash of the plain-text API key. The raw key is not stored. |
| `Prefix` | `string` | Short visible identifier shown in the admin UI. |
| `Scopes` | `List<string>?` | Stored permission keys that define what the key can do. |
| `OrganizationId` | `Guid?` | Optional organization link for administrative tracking and future org-aware use cases. |
| `ExpiresAt` | `DateTime?` | Optional expiration timestamp. |
| `LastUsedAt` | `DateTime?` | Last successful authentication time. |
| `IsRevoked` | `bool` | Permanent revocation flag. |
| `RevokedAt` | `DateTime?` | Time the key was revoked. |

### Important design detail

`Scopes` are now treated as the key’s own permission snapshot.

That means:

- the key does not dynamically inherit future owner role changes
- old integrations remain stable when the owner is promoted or demoted
- leaving scopes empty during creation does not mean unlimited future access

Instead, empty scopes at creation time are converted into a snapshot of the creator’s current effective permissions.

## EF Core Configuration

The database configuration is defined in `ApplicationDbContext`.

### Current configuration

- `Scopes` is stored as `jsonb`
- `CreatedById` points to `UserProfile` with `DeleteBehavior.Restrict`
- `OrganizationId` points to `Organization` with `DeleteBehavior.Cascade`
- unique index on `KeyHash`
- non-unique index on `Prefix`

### Implications

- duplicate key hashes cannot exist
- the visible prefix is searchable but not guaranteed unique
- deleting an organization deletes keys associated with that organization
- deleting the creator profile is restricted while keys still reference it

## Shared Models and Validation

### ApiKeyItem

This is the shared admin/service DTO for listing and creation.

Validation rules:

| Field | Rules |
|---|---|
| `Name` | required, min length `2`, max length `256` |

The DTO also carries:

- `Prefix`
- `Scopes`
- `OrganizationId`
- `OrganizationName`
- `ExpiresAt`
- `LastUsedAt`
- `IsRevoked`
- `RevokedAt`
- `CreatedByName`
- `CreateDate`

### ApiKeyCreateResult

Key creation returns:

- the normal `ApiKeyItem`
- `PlainTextKey`

The raw key is only available at creation time. It is not stored and cannot be shown again later.

## Key Generation and Storage

Key creation happens in `ApiKeyAdminService`.

### Generated key format

The current format is:

```text
dca_<40 lowercase hex characters>
```

The value is built from:

- the fixed `dca_` prefix
- 20 random bytes
- lowercase hex encoding

### Stored values

When a key is created:

- the plain-text key is generated once
- `Prefix` is set to the first 8 characters of the generated value
- `KeyHash` is stored as SHA-256 hex
- the plain-text key is returned only in the create response

This means the database is usable for verification and admin auditing, but not for recovering the original secret.

## Authentication Flow

API key authentication is implemented by `ApiKeyAuthenticationHandler`.

### Header

Clients authenticate with:

```http
X-Api-Key: dca_...
```

### Validation sequence

On each request, the handler:

1. reads `X-Api-Key`
2. hashes the supplied key with SHA-256
3. loads the matching `ApiKey` by `KeyHash`
4. rejects the request if the key does not exist
5. rejects inactive keys
6. rejects revoked keys
7. rejects expired keys
8. resolves the owner `ApplicationUser`
9. rejects the request if the owner does not exist
10. rejects the request if the owner account is not `Active`

If validation succeeds, the request becomes authenticated as the owning user identity, with an `ApiKeyId` claim attached.

### Bearer precedence

The smart auth selector now prefers JWT bearer auth before API key auth.

That matters when a request accidentally includes both:

- `Authorization: Bearer ...`
- `X-Api-Key: ...`

In that case, bearer auth wins and the request does not get hijacked by an unrelated or stale API key header.

## Authorization and Scopes

Authorization still uses the normal permission-policy system.

### How API key permissions are applied now

For API-key-authenticated requests:

- `PermissionClaimsTransformation` detects the `ApiKeyId` claim
- it loads the key’s stored `Scopes`
- it creates `Permission` claims directly from those stored values
- it does not rebuild permissions from the owner’s current roles

This is the key behavioral rule in the current implementation:

> API key authorization comes from the key record, not from the owner’s live role membership.

### Why this changed

Earlier behavior allowed old keys to silently gain or lose access when the owner’s roles changed. That made integrations unstable and made least-privilege reviews hard to reason about.

The current approach is safer:

- every key has its own explicit permission set
- owner role changes do not broaden existing keys
- keys can still be revoked centrally by revoking the key or deactivating the owner account

## Scope Resolution on Create

When creating a key, the admin UI accepts a comma-separated list of permission keys.

### If scopes are supplied

The service:

- trims each value
- removes blanks
- de-duplicates them
- stores the final list in sorted order

### If scopes are left empty

The service snapshots the creator’s current effective permissions by:

1. loading the creator’s `ApplicationUserId`
2. loading all current role memberships
3. resolving role permissions
4. applying user permission overrides
5. storing the resulting permission list on the key

That means “empty scopes” currently means:

`use my current permissions right now as the key's stored permission set`

It does not mean:

- unlimited access
- dynamic future access
- bypass permission checks

## Legacy Backfill Behavior

The starter includes `ApiKeyDataSeeder` to handle older keys created before scope snapshotting existed.

On startup it:

- finds keys where `Scopes` is `null` or empty
- resolves the owner’s current effective permissions once
- stores that permission list on the key

This makes old keys stable going forward without requiring manual re-creation.

### Operational implication

After this change, legacy keys stop drifting with owner role changes. Whatever permission set they receive during backfill becomes their stable scope set unless the key is revoked and recreated.

## Organization Association

The current implementation now exposes optional organization association in the admin contract and UI.

### What organization currently does

Today it primarily provides:

- administrative grouping
- future-proofing for org-aware integrations
- parity with other org-capable features in the starter

### What it does not currently do by itself

The API key’s `OrganizationId` does not automatically change the request’s operation context or override the owner’s visible organizations.

So today it should be treated as:

- meaningful metadata
- useful for management and future extension

but not as a full tenant-isolation boundary by itself.

If a project needs strict org-bound machine auth, this area should be extended further so request context is derived from the key as well.

## Admin UI

The admin page is available at:

```text
/admin/api-keys
```

### Current capabilities

- list keys
- search by name or prefix
- view prefix, scope count, organization, creator, expiry, last-used time, and status
- create new keys
- copy the raw key once immediately after creation
- revoke keys

### Current create fields

- `Name`
- `Scopes`
- `Organization Public ID`
- `Expires At`

### Current limitations

- no edit flow after creation
- no secret reveal after creation
- no rotate action
- no per-key description field
- no key usage audit beyond `LastUsedAt`

Revocation is permanent in the current starter behavior.

## Permissions

The admin feature uses these permission keys:

| Permission | Meaning |
|---|---|
| `Admin.ApiKeys.View` | View the API key page and list keys |
| `Admin.ApiKeys.Create` | Create new API keys |
| `Admin.ApiKeys.Revoke` | Revoke existing API keys |

These are admin-management permissions. They are separate from the permissions stored on the keys themselves.

## Runtime Caveats

Developers using this starter should know these current constraints:

### 1. Keys are create-and-revoke, not full lifecycle managed

You can create and revoke keys, but not:

- rotate in place
- rename with history
- edit scopes after creation
- recover a lost key

If an integration needs a changed permission set, the intended workflow is to create a new key and revoke the old one.

### 2. Last-used tracking is best-effort

`LastUsedAt` is updated after successful auth in a fire-and-forget path. It is useful operationally, but it should not be treated as a strict audit ledger.

### 3. Org association is not full org execution context

The key can be linked to an organization record, but request context still fundamentally resolves through the owning user.

### 4. Keys still depend on the owner account existing and being active

This is intentional. If the owning account is deactivated, its keys stop working too.

## How to Use It in Application Code

From a client or integration, call the application with:

```http
X-Api-Key: dca_your_generated_key_here
```

From the server side, continue using normal authorization policies. API key requests end up with the same `Permission` claim shape as other authenticated requests, so existing policy-based checks continue to work.

Examples:

- `[Authorize(Policy = "Admin.Users.View")]`
- `[Authorize(Policy = "System.Jobs.View")]`

As long as the key’s stored `Scopes` contain the required permission key, the request can satisfy that policy.

## Recommended Usage Pattern

For teams starting from DevCoreApp, the safest operating model is:

- create separate keys per integration
- explicitly supply a narrow scope list when possible
- use expiration dates for temporary or external integrations
- treat empty scopes as a convenience snapshot, not as the default best practice
- revoke and recreate keys instead of trying to mutate them in place

## Summary

The current API key implementation is a solid starter-level machine authentication feature with hashed storage, permission snapshots, owner-state enforcement, revocation, and basic admin management.

Its main strengths are:

- simple operational model
- compatibility with the existing permission system
- safer stable authorization than the earlier role-drifting design

Its main deliberate limitations are:

- no rotation workflow
- no post-create editing
- no full org-context enforcement from the key itself
- best-effort rather than audit-grade usage tracking

For many internal tools and first-party integrations, that is enough. Projects with more advanced integration security requirements can extend this foundation later.
