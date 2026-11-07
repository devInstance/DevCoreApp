---
origin: ThreadIQ
targets: [DevCoreApp]
scope: []   # no shared code — CLAUDE.md files are product-local; this is guidance for the template's own docs
status: pending
change: "ThreadIQ commit 'Split root CLAUDE.md into area guides' on dev/claude-md-split (docs only)"
related:
  - CLAUDE.md                               # ThreadIQ-local: the trimmed root
  - src/Server/Services/App/CLAUDE.md       # ThreadIQ-local: where the product domain went
---

# Keep the root CLAUDE.md to cross-cutting rules; push area detail into nested CLAUDE.md files

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-10-08 | Authored in ThreadIQ `out/`. Not yet delivered. Advisory only — no code to apply. |

## Why

Claude Code loads the root `CLAUDE.md` into **every** session, before any work. A nested `CLAUDE.md`
loads only when files in its folder are read. ThreadIQ's root had grown to **83 KB (~20k tokens)**,
mostly product-domain notes (CRM model, AI subsystem) that matter only when working in those folders,
plus the same rule stated two to four times and dated snapshots ("580 tests", "four fenced spots",
"five docs in `out/`") that had silently gone stale. It is now a 53 KB root plus area guides.

## Current state (DevCoreApp, checked 2026-10-08)

DevCoreApp's root `CLAUDE.md` is **38 KB** and carries no product domain, so it does **not** need the
split today. What it does have, on a smaller scale, is the same drift:

| Spot | Issue |
|---|---|
| "Build, Test, and Run Commands" | `The suite is small (~75 tests)` — a snapshot count that will rot |
| "Things To Never Do" | `Never share one DbContext…` repeats "Data Access Pattern"; `Never instantiate entities directly…` repeats the `query.CreateNew()` line in "Things To Always Do" |
| "Feature Folder Organization" | ~2 KB, of which the `Database/Core` tree is the only part not discoverable with `ls` |

## Proposed change

1. **Adopt the rule, so forks inherit it:** the root `CLAUDE.md` holds commands, the Core/App split,
   dependency rules, data access + org scoping, date/time, and the always/never lists. Anything that
   matters only inside one area goes into a `CLAUDE.md` in that folder, and the root's docs table
   links it with a *"read when …"* trigger. Product forks put their domain model in
   `src/Server/Services/App/CLAUDE.md` (ThreadIQ's choice) rather than in the root.
2. **Use the same nested locations as ThreadIQ**, so a fork's guides line up with the template's:
   | File | Holds |
   |---|---|
   | `src/Server/Database/Core/CLAUDE.md` | Entity base classes, layer-first layout, why shared queries stay flat (sync key) |
   | `src/Shared/CLAUDE.md` | Phone/URL normalization strategies (if DevCoreApp has them) |
   | `src/Client/CLAUDE.md` | Rules common to both clients: JWT auth, `<img>`/bearer, hosting, client-side date conversion, the TS/SCSS pipeline |
   | `tests/CLAUDE.md` | Test conventions — no fixtures, where `TestUtils`/`ServerTestUtils` live, testing without a DB |
   DevCoreApp's `src/Server/Database/UnitOfWork.md` could become (or be linked from) the Database guide.
3. **Drop snapshot numbers and dated counts** — say "list the folder" or "grep for the keyword"
   instead of "five docs" / "four fenced spots" / "~75 tests".
4. **State each rule once.** Keep the explanation in its section and a one-line entry in the
   always/never list only if the section doesn't already make it unmissable.

## Affected shared surface

None. `CLAUDE.md` files are product-local and never synced; each repo applies this to its own docs.

## Source change

`git show <sha> -- CLAUDE.md src/**/CLAUDE.md tests/CLAUDE.md` in ThreadIQ shows the split. It is a
reference for the shape only — the content is ThreadIQ's own and must not be copied.

## Verification

Root `CLAUDE.md` size before/after (`wc -c`); every relocated section reachable from the root's docs
table; `grep` the root for any count or "as of" date and confirm each is still needed.
