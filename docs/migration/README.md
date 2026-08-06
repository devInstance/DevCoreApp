# Cross-Project Migration Workflow

DevCoreApp is a **template**. Downstream products (**ThreadIQ**, **Tentrie**, …) are forks of it. Shared/template code lives under `Core` namespaces/folders; product-specific code lives under `App` (see the "Shared Core vs Product Code" section in the root [`CLAUDE.md`](../../CLAUDE.md)).

A fix made to shared (`Core`) code in *any* repo needs to reach *every* repo. This folder is how those fixes travel: as human- and agent-readable **instruction docs**, not as raw patches (the same logical file has a different product namespace prefix in each fork, so a literal diff rarely applies cleanly).

## Hub-and-spoke

DevCoreApp is the **hub** and canonical owner of `Core`. Forks are spokes.

```
   Tentrie /out ─┐                        ┌─► ThreadIQ /in
                 ├─► DevCoreApp /in ─► apply to canonical Core ─► DevCoreApp /out ─┤
   ThreadIQ /out ┘                        └─► Tentrie  /in
```

Every repo (template + each fork) has the same two folders:

- **`in/`** — pending instruction docs authored *elsewhere*, to be applied *here*. `status: pending` until done.
- **`out/`** — instruction docs authored *here* for the *other* repos to consume. Each target copies the doc into its own `in/`.

Transport between repos is a manual copy / PR — there is no shared package feed.

## Lifecycle

1. **Author.** You fix something in `Core` (or spot a needed change). Copy [`_template.md`](_template.md) to `<repo>/docs/migration/out/YYYY-MM-DD-<slug>.md` and fill it in. Set `status: pending`.
2. **Deliver.** Copy that file into each target repo's `in/` (a fork targets DevCoreApp upstream; DevCoreApp targets every fork). Keep the same filename.
3. **Apply.** In the receiving repo, implement the change against its `Core` code, then set `status: applied` and move the file to `in/applied/`.
4. **Fan out (hub only).** After DevCoreApp applies an inbound fix to canonical `Core`, author the outbound equivalent(s) in DevCoreApp's `out/` for the remaining forks and repeat from step 2.

## Naming & front-matter

- **Filename:** `YYYY-MM-DD-<short-slug>.md`
- **Front-matter** (see `_template.md`): `origin`, `targets`, `scope`, `status`, `related`.
- The `scope` lists the affected **shared surface** by sync key — the namespace with the `DevInstance.{Product}` prefix stripped (e.g. `Server.Database.Core.Models.FileRecord`) — so a reader in any fork can locate the same file regardless of its product prefix.

## Applying a doc that touches deviating code

If the receiving repo has a `#region project-specific <Product>: …` fence around the exact lines an inbound doc changes, **preserve the fenced content** — the fence marks a deliberate local override. Reconcile by hand and note it in the doc's Open Questions before marking it applied.
