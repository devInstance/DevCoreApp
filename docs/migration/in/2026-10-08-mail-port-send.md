---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  # Port (EmailIntegration.Abstractions.csproj)
  - Server.Integrations.Email.Core.IMailProvider              # + SendAsync, class docs
  - Server.Integrations.Email.Core.MailProviderCapabilities   # + Send = 1 << 4
  - Server.Integrations.Email.Core.MailOutgoingMessage        # new
  - Server.Integrations.Email.Core.MailSendResult             # new
  - Server.Integrations.Email.Core.MailSendOutcomeUnknownException   # new, in MailProviderExceptions.cs
  - Server.Integrations.Email.Core.UnavailableMailProvider    # + SendAsync (throws)
  # Microsoft Graph adapter (EmailIntegration.Graph.csproj)
  - Server.Integrations.Email.Graph.Core.GraphMailProvider    # + SendAsync, Send capability, injectable delay
  - Server.Integrations.Email.Graph.Core.IGraphMailClient     # + SendMailAsync, FindSentMessageAsync
  - Server.Integrations.Email.Graph.Core.GraphMailClient
  - Server.Integrations.Email.Graph.Core.GraphMessageMapper   # new partial: GraphMessageMapper.Outgoing.cs
  - Server.Integrations.Email.Graph.Core.GraphMailSettings    # + Mail.Send scope, AlwaysRequested list
  - Server.Integrations.Email.Graph.Core.GraphRetryPolicy     # + idempotent flag, IsServerFault
  # Tests (EmailIntegration.Graph.Tests.csproj)
  - Server.Integrations.Email.Graph.Tests.Core.FakeGraphMailClient
  - Server.Integrations.Email.Graph.Tests.Core.GraphMailSendTests   # new, 15 tests
status: pending
change: "190cf71"   # ThreadIQ dev/1194-send-email-from-app, "#1194: Sending email via the App - phase 3" — the only commit on the branch touching src/Server/Integrations or tests/Server/Integrations
related:
  - docs/migration/out/2026-10-07-inbound-mail-calendar-integrations.md   # PREREQUISITE — this doc extends those projects
  - docs/outbound-messaging-plan.md        # ThreadIQ-local: the whole feature, decisions A–C, why B1
  - docs/email-integration-entra-setup.md  # ThreadIQ-local: the Mail.Send delegated permission
---

# The mail port can send, as the connected mailbox (Microsoft Graph `sendMail`)

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-10-08 | Authored in ThreadIQ `out/` under `#1194`. Not yet delivered. **Deliver after** `2026-10-07-inbound-mail-calendar-integrations.md` — that doc brings the projects this one changes. |

## Why

`IMailProvider` was read-only: connect a user's mailbox over OAuth, read and search it. Products
built on it want the obvious next step — let the user send a message **from their own mailbox** on
an explicit action, so the recipient sees a normal email from that person and the copy sits in their
Sent Items. That is vendor-specific in exactly the way the rest of the port is (same provider key,
same credential, same consent), so it belongs on the same port rather than in a second registry.

It is **not** `IEmailProvider` in `src/Server/Email/Processor` — that sends the application's own mail
(notifications, resets) through SMTP/SendGrid. The two still share no types; the port's class doc
says so.

Nothing in this change is ThreadIQ-specific. What ThreadIQ does with it (recording the sent message
on a CRM deal, a channel abstraction, the composer UI, a permission) lives in its `App/` consumers
and is not part of this doc.

## Current state (ThreadIQ, after this change)

| Surface | Behaviour |
|---|---|
| `MailProviderCapabilities.Send` | New flag. `GraphMailProvider` reports it; `UnavailableMailProvider` and any provider without it throws `MailCapabilityNotSupportedException` from `SendAsync`. |
| `IMailProvider.SendAsync(credential, MailOutgoingMessage, ct)` → `MailSendResult` | Sends as the credential's mailbox, keeping a Sent Items copy. |
| `MailOutgoingMessage` | `To` (required), `Cc`, `Subject`, `BodyText` (plain text in v1), **`InternetMessageId` (required, set by the caller)**, `InReplyToInternetMessageId` (reserved for reply threading, unused by the Graph adapter in v1). `NewInternetMessageId(senderAddress)` builds `<{guid:N}@{sender domain}>`. |
| `MailSendResult` | `InternetMessageId`, `IsConfirmed`, `ProviderMessageId`, `ThreadId`, `WebLink`, `SentAtUtc`. |
| `MailSendOutcomeUnknownException` | New `MailProviderException`: the send may or may not have gone out. Callers must not retry. |
| Graph send | `POST /me/sendMail` with `saveToSentItems = true` and the caller's `internetMessageId` in the payload. **Verified live 2026-10-08 against Microsoft 365: Graph keeps a caller-set `internetMessageId` on `sendMail`.** So only `Mail.Send` is needed — not the `Mail.ReadWrite` a draft-then-send flow would require. |
| Confirmation | After a successful `sendMail`, up to three Sent Items lookups (`internetMessageId eq '…'`, quotes doubled) after 0.5 s / 1.5 s / 3 s fill `ProviderMessageId`/`ThreadId`/`WebLink` and set `IsConfirmed`. Best effort, uncancelled, and **never throws** — the message is already out. |
| Retry | `GraphRetryPolicy.ExecuteAsync(..., idempotent: false)` for the send: only 429 is retried (Graph refused without acting); a 5xx becomes `MailSendOutcomeUnknownException` instead of a retry that could deliver twice. Any non-`MailProviderException` thrown by the send call (network reset, timeout) is likewise reported as outcome-unknown. |
| Consent | `https://graph.microsoft.com/Mail.Send` added to `DefaultScopes`. A new `AlwaysRequested` list (`Calendars.Read`, `Mail.Send`) replaces the calendar-only special case: each is appended to a configured scope list that lacks it, matched by name, so deployments with an explicit list still get it on the next connect. Mailboxes connected earlier keep their narrower grant until reconnected. |

## Proposed change

Apply the same surface to DevCoreApp's copies of these projects:

1. **Port** — add the capability flag, the two model types, the exception, and `SendAsync` on
   `IMailProvider` (with the "Sending (optional)" section and the updated class doc). Add
   `SendAsync` to `UnavailableMailProvider` (throws its usual unavailable error).
2. **Graph** — `IGraphMailClient.SendMailAsync(message, ct)` / `FindSentMessageAsync(internetMessageId, ct)`
   and their implementation in `GraphMailClient` (`Me.SendMail.PostAsync` with
   `SendMailPostRequestBody { Message, SaveToSentItems = true }`; `Me.MailFolders["sentitems"].Messages`
   filtered by `internetMessageId`, `Top = 1`). `GraphMessageMapper.Outgoing.cs` maps
   `MailOutgoingMessage` to a Graph `Message` (text body, To/Cc recipients, `InternetMessageId`).
   `GraphMailProvider.SendAsync` validates (at least one To, an `InternetMessageId`), sends with
   `idempotent: false`, then confirms as above. Its constructor gains an optional
   `Func<TimeSpan, CancellationToken, Task>? delay` so tests do not sleep.
3. **Retry policy** — the `idempotent` parameter (default `true`, so every existing call is
   unchanged) and `IsServerFault`.
4. **Scopes** — `Mail.Send` in `DefaultScopes` and the `AlwaysRequested` loop.
5. **Every other `IMailProvider` implementation in the receiving repo** — test fakes included —
   needs a `SendAsync`. A fake that never sends can throw `NotImplementedException`.

Operators of a deployment that wants sending must add the **delegated `Mail.Send`** permission to
the Entra app registration (some tenants require admin consent for it), and users reconnect their
mailbox once to grant it.

## Affected shared surface

As listed in `scope`. No `#region project-specific` fence touches these files in ThreadIQ.

One product reference to drop on the way in: the comment above `Mail.Send` in
`GraphMailSettings.DefaultScopes` says "Sending from the app (#1194)" — `#1194` is a ThreadIQ issue
number. Keep the rest of the comment.

## Source change

```bash
git show 190cf71 -- src/Server/Integrations tests/Server/Integrations
```

Everything else in that commit is ThreadIQ-local and must **not** be taken upstream — all of it
under an `App/` folder or in `docs/`: the "Send test email" action (`Services/App/Mail/MailAccountService`,
`IMailAccountService`, `Controllers/Integrations/MailAccountsController`, the client service and the
Desktop mail connections page), `MailAccountItem.CanSend` + `MailAccountDecorators`,
`MailTestSendItem`, the `tests/Server/Api/App/*` fakes, and the two docs. No other commit on the
branch touches `src/Server/Integrations` or `tests/Server/Integrations`.

The diff is a reference, not a patch: namespaces carry `DevInstance.ThreadIQ.` here and a different
prefix in every fork.

## Verification

- `dotnet build` — 0 errors. Every `IMailProvider` implementation compiles only once it has `SendAsync`.
- `dotnet test` on `EmailIntegration.Graph.Tests` — `GraphMailSendTests` (15) covers: the Send
  capability; a plain-text payload under the caller's Message-ID; Sent Items confirmation found / not
  found / lookup failing (the last two unconfirmed, never an error); an interrupted send reported as
  outcome-unknown; a missing send grant as authorization-required; no recipients refused before
  Graph is called; unique Message-IDs on the sender's domain; a 5xx on a send **not** retried while a
  5xx on a read still is; 429 on a send retried; `Mail.Send` in the default scopes and appended to a
  configured list that lacks it, without duplicating a short-form entry. In ThreadIQ the project runs
  83 tests, all green.
- Manual, against a real Microsoft 365 mailbox with `Mail.Send` granted: send a message to the
  mailbox itself and confirm `IsConfirmed = true` (the copy is in Sent Items under the requested
  Message-ID). ThreadIQ did this through its "Send test email" action on the mail connections page;
  DevCoreApp has no such page and needs a scratch caller or its own equivalent.

## Open questions

- **HTML bodies.** v1 sends plain text (`BodyType.Text`). A signature with formatting or links will
  want HTML; adding `BodyHtml` to `MailOutgoingMessage` is the expected next shared change
  (ThreadIQ's signature work is tracked in its `docs/open-items.md`).
- **Reply threading.** `InReplyToInternetMessageId` is on the model but the Graph adapter ignores it.
  Threading through Graph means `createReply` on the original message (which needs `Mail.ReadWrite`)
  or setting `In-Reply-To`/`References` headers on `sendMail`; not yet decided.
