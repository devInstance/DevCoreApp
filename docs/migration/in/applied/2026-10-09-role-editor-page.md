---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Client.Desktop.Core.UI.Pages.Admin.RolesPage   # RolesPage.razor + RolesPage.razor.cs — edit modal removed, row click / Edit button navigate
  - Client.Desktop.Core.UI.Pages.Admin.EditRole    # new: EditRole.razor + EditRole.razor.cs at /admin/roles/{RoleId}/edit
status: applied
change: "f6b30f9 c013e04"   # ThreadIQ main: "#1223: Roles no longer shows permissions" (bug fix) and "- phase 2" (modal → page). c013e04 supersedes the f6b30f9 change; take both for the record, apply the end state.
related:
  - ThreadIQ issue #1223 "Roles no longer shows permissions"
---

# Role editing moves from a modal to its own page (and the row-click spinner bug goes with it)

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-10-09 | Authored in ThreadIQ `out/` under `#1223`. Not yet delivered. |
| 2026-11-07 | Applied in DevCoreApp (c013e04 end state). Deviation: `RolesPage` keeps DevCoreApp's inline grid-profile load/save loops — ThreadIQ's `ApplyProfile`/`ToColumnStates` helpers (`GridProfileColumnExtensions`) are not in DevCoreApp and are out of scope. |

## Why

Two problems on `/admin/roles`, both in code DevCoreApp ships (it came over unchanged in the WASM
migration):

1. **Bug — clicking a row opened the edit modal on an endless spinner.** `RolesPage.OnRowClick`
   called the async `ShowEditModal(item)` without awaiting it and returned `Task.CompletedTask`.
   The modal rendered (the synchronous part of `ShowEditModal` set `EditingRole`), but the permission
   catalog arrived after the event handler had already finished, so nothing re-rendered the page and
   the modal stayed on its "Loading permissions..." spinner. The pencil **Edit** button worked only
   because its lambda returned the `Task`, which Blazor awaits.
2. **Layout — the permission matrix does not fit a modal.** `PermissionGrid` renders one card per
   module with a column per action; inside `modal-lg` it is cramped and scrolls inside the dialog.

Nothing here is product-specific: the page, the client service and the endpoints are all Core.

## Current state (DevCoreApp, before this change)

| Surface | Behaviour |
|---|---|
| `RolesPage` row click | Opens the edit modal; permissions never appear (bug 1). |
| `RolesPage` Edit button | Opens the same modal; works. |
| Edit modal | Name, description, `PermissionGrid`, Save/Cancel, inside `modal-dialog modal-lg`. Holds `EditingRole`, `allPermissions`, `selectedPermissionKeys`, `ShowEditModal`, `CloseEditModal`, `SaveRoleAndPermissions` on `RolesPage`. |

## Proposed change

No server, API, DTO or client-service change — every call the page needs already exists
(`IRoleManagementService.GetRoleAsync`, `GetAllPermissionsAsync`, `GetRolePermissionKeysAsync`,
`UpdateRoleAsync`, `SetRolePermissionsAsync`; `GET api/roles/{id}` etc. on `RolesController`).

1. **New page `Pages/Admin/EditRole.razor` (+ `.razor.cs`)** at `@page "/admin/roles/{RoleId}/edit"`
   (same URL shape as `/admin/users/{UserId}/edit`), `[Authorize(Policy = PermissionDefinitions.Admin.Roles.Edit)]`.
   - "← Roles" back link; a header row with the role name (+ `System` badge) and Cancel / Save
     Changes buttons, so Save is reachable without scrolling past the matrix.
   - The system-role info alert, then Name (`col-md-4`) and Description (`col-md-8`) side by side.
   - "Permissions — *n* of *m* selected", then the existing `PermissionGrid` full page width,
     `ReadOnly` for a system role.
   - `OnParametersSetAsync` loads role → permission catalog → role's keys in sequence, stopping at
     the first `Host.IsError` (an unknown id shows the error, not a spinner).
   - `SaveRoleAndPermissions` is the modal's logic moved over unchanged (update the role; if not a
     system role, `SetRolePermissionsAsync`), then `NavigationManager.NavigateTo("/admin/roles")`.
2. **`RolesPage`**
   - Delete the whole "Edit + Permissions Modal" markup block and its state/methods
     (`EditingRole`, `allPermissions`, `selectedPermissionKeys`, `ShowEditModal`, `CloseEditModal`,
     `SaveRoleAndPermissions`).
   - Inject `NavigationManager`; add
     ```csharp
     private void OnRowClick(RoleItem item) => EditRole(item);

     private void EditRole(RoleItem role) =>
         NavigationManager.NavigateTo($"/admin/roles/{Uri.EscapeDataString(role.Id)}/edit");
     ```
     and point the Edit button at `EditRole(role)`. Navigating synchronously is what removes bug 1 —
     there is no longer an async load behind the row click.
   - The Create and Delete modals stay as they are (small forms; they fit).

The route is not added to `NavMenu.razor` — it is reached only from the list.

## Affected shared surface

- `Client.Desktop.Core.UI.Pages.Admin.RolesPage` (`RolesPage.razor`, `RolesPage.razor.cs`)
- `Client.Desktop.Core.UI.Pages.Admin.EditRole` (new)

ThreadIQ has no `project-specific` fence in either file. If a fork has fenced the edit modal, carry
the fenced content into `EditRole.razor`.

## Source change

```bash
git show f6b30f9 c013e04 -- src/Client/ThreadIQ.Client.Desktop/Core/UI/Pages/Admin/
```

`f6b30f9` only made `OnRowClick` return `ShowEditModal(item)`; `c013e04` replaces that with the
navigation above, so apply the `c013e04` end state. The diff is a reference, not a patch — namespaces
carry `DevInstance.ThreadIQ` where DevCoreApp has its own prefix. This doc itself is ThreadIQ-local.

## Verification

- `dotnet build` (Desktop client).
- Manual, against a running host: `/admin/roles` → click a **row** → lands on
  `/admin/roles/{id}/edit` with the matrix filled in; the pencil button does the same.
  Change a permission on a custom role, Save → back on the list with the Permissions count updated.
  Open a system role → name and checkboxes disabled, description editable.
  `/admin/roles/does-not-exist/edit` → error alert, no spinner.
- Under `-c ServiceMocks`, `RoleManagementServiceMock.GetRoleAsync` already exists; nothing to add.
