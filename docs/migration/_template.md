---
origin: <Product that authored this — e.g. Tentrie | DevCoreApp>
targets: [<repos that should apply this — e.g. DevCoreApp | ThreadIQ, Tentrie>]
scope:
  - <affected shared surface, by sync key — e.g. Server.Database.Core.Models.FileRecord>
status: pending   # pending | applied
related:
  - <links to PRs, issues, or sibling migration docs>
---

# <Title — what changes, in one line>

## Why

<The problem or need. Why this belongs in shared `Core`, not just one product.>

## Current state

<How the affected surface behaves today in the origin repo. A small table of
surface / pattern / consequence works well. Point at concrete files by sync key.>

## Proposed change

<The change to make. Concrete: interfaces, method shapes, EF config, registration.
Include short code snippets where they remove ambiguity. State decisions, not options.>

## Affected shared surface

<List every `Core` file to touch, by sync key. Flag anything that may collide with a
local `#region project-specific` fence in a receiving fork.>

## Verification

<How the receiving repo confirms the change is correct: build command, the specific
tests to run/add, and any manual check.>

## Open questions

<Anything unresolved. Delete the section if there are none.>
