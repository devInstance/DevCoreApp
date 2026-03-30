# Email System

## Overview

The email system provides asynchronous outbound email delivery for account flows and admin-triggered notifications. In the current starter implementation, it supports:

- template-based HTML and text emails
- background-queued delivery
- provider-based sending through `IEmailProvider`
- email delivery logging
- admin visibility into queued, sent, and failed emails
- manual resend of failed emails
- health monitoring for stuck queued emails

The system is designed so application code queues email work quickly and the background worker performs the actual delivery later.

## Why This Feature Exists

This starter includes a built-in email pipeline so new projects can handle common account and operational emails without wiring up a mail provider from scratch.

Typical uses in the current codebase include:

- account email confirmation
- password reset links
- password reset codes
- invited-user registration emails

## High-Level Design

The email feature is split across configuration, templating, background processing, provider delivery, and admin log visibility.

| Area | Responsibility | Main files |
|---|---|---|
| Provider abstraction | Defines the delivery contract | `src/Server/Email/Processor/IEmailProvider.cs` |
| Provider registration | Registers the active mail provider in DI | `src/Server/Admin/WebService/Program.cs` |
| Mail provider implementations | Connects to SMTP-style services | `src/Server/Email/MailKit/ConfigurationExtensions.cs`, `src/Server/Email/MailKit/MailKitEmailSender.cs`, `src/Server/Email/Smtp/SmtpEmailProvider.cs` |
| Identity email sender | Queues account-related emails | `src/Server/Admin/Services/Notifications/IdentityEmailSender.cs` |
| Template rendering | Resolves template files and replaces placeholders | `src/Server/Admin/Services/Notifications/Templates/EmailTemplateService.cs` |
| Background queue | Creates `EmailLog` rows and `BackgroundTask` jobs | `src/Server/Admin/Services/Background/BackgroundWorker.cs` |
| Delivery handler | Sends queued email and updates log status | `src/Server/Admin/Services/Background/Tasks/Handlers/SendEmailTaskHandler.cs` |
| Email log admin service | Lists, filters, deletes, and resends log entries | `src/Server/Admin/Services/Email/EmailLogService.cs` |
| Admin UI | Email log list and detail pages | `src/Server/Admin/WebService/UI/Pages/Admin/EmailLog.razor`, `src/Server/Admin/WebService/UI/Pages/Admin/EmailLogDetail.razor` |
| Health check | Detects stale queued emails | `src/Server/Admin/WebService/Health/StuckEmailsHealthCheck.cs` |

## Active Provider

The web app currently registers `MailKit` as the active provider in `Program.cs`:

```csharp
builder.Services.AddMailKit(builder.Configuration);
```

That means current runtime delivery goes through:

- `MailKitEmailSender`
- SMTP server credentials from configuration

Other provider code exists, but it is not the active runtime path by default.

## Configuration

The default configuration block lives in `src/Server/Admin/WebService/appsettings.json`:

```json
"EmailConfiguration": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 465,
  "Username": "admin@devinstance.net",
  "Password": "test password",
  "FromEmail": "noreply@devinstance.net",
  "FromName": "DevCoreApp"
}
```

### Fields

| Key | Meaning |
|---|---|
| `EmailConfiguration:SmtpServer` | SMTP host name |
| `EmailConfiguration:Port` | SMTP port |
| `EmailConfiguration:Username` | SMTP login user in appsettings |
| `EmailConfiguration:Password` | SMTP login password |
| `EmailConfiguration:FromEmail` | Default sender address for identity-driven emails |
| `EmailConfiguration:FromName` | Default sender display name for identity-driven emails |

### Important configuration caveat

The current provider registration code reads:

- `EmailConfiguration:UserName`

but `appsettings.json` currently uses:

- `EmailConfiguration:Username`

Because configuration keys are case-insensitive but not spelling-insensitive, `UserName` and `Username` are different keys. As written, the provider setup can miss the configured username unless you supply the exact key the code expects.

For a working setup, make sure your real environment provides:

```json
"EmailConfiguration": {
  "SmtpServer": "...",
  "Port": 587,
  "UserName": "...",
  "Password": "...",
  "FromEmail": "...",
  "FromName": "..."
}
```

### Environment guidance

For real deployments, do not keep SMTP credentials in checked-in `appsettings.json`. Use environment-specific configuration or secrets management.

## Provider Implementations

### MailKit

`MailKitEmailSender`:

- builds a `MimeMessage`
- copies all `To` recipients into the message
- connects to the configured SMTP host
- authenticates with username and password
- sends the message
- returns the provider response string as `ProviderId`

### SMTP provider

`SmtpEmailProvider` is included as an alternative provider option in the starter. It is not the default active provider in `Program.cs`, but teams using DevCoreApp as a starting project can switch to it or extend it if they prefer that path.

### SendGrid provider

`SendGridEmailProvider` exists as a placeholder in the starter. It is not implemented yet, and teams using DevCoreApp as a starting project can implement it later if they want SendGrid support.

## Template System

Templates are registered in `EmailTemplateRepository` and rendered by `EmailTemplateService`.

### Registered templates

| Template name | Subject | File |
|---|---|---|
| `Registration` | `Complete Your Registration` | `wwwroot/email-templates/registration.html` |
| `ConfirmEmail` | `Confirm your email` | `wwwroot/email-templates/confirm-email.html` |
| `PasswordResetLink` | `Reset your password` | `wwwroot/email-templates/password-reset-link.html` |
| `PasswordResetCode` | `Reset your password` | `wwwroot/email-templates/password-reset-code.html` |

### How rendering works

`EmailTemplateService.RenderAsync`:

1. looks up the template descriptor by logical name
2. loads the template file from `wwwroot`
3. replaces `{{Placeholder}}` tokens in both subject and body
4. returns `EmailTemplateResult`

This is a simple token-replacement system. It does not currently support:

- loops
- conditionals
- layout inheritance
- escaping rules

## Email Request Model

Background email payloads are serialized as `EmailRequest`.

```csharp
public class EmailRequest : IEmailMessage
{
    public EmailAddress From { get; set; } = new();
    public List<EmailAddress> To { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EmailLogId { get; set; }
    public string? TemplateName { get; set; }
}
```

### Current recipient rule

The system now enforces exactly one recipient per email request.

Both `EmailSenderService` and `SendEmailTaskHandler` reject requests where `To.Count != 1`.

This is intentional because the persisted `EmailLog` model stores only one destination address and one destination name. Multi-recipient support would require a different log model.

## Delivery Flow

The runtime flow is:

1. application code creates or renders an email
2. code submits `BackgroundRequestType.SendEmail`
3. `BackgroundWorker.SubmitAsync` creates an `EmailLog` row if one does not already exist
4. `BackgroundWorker` persists a `BackgroundTask` with `ResultReference = EmailLog:{publicId}`
5. the background task worker claims queued jobs
6. `SendEmailTaskHandler` deserializes the `EmailRequest`
7. the handler loads the linked `EmailLog`
8. the active `IEmailSenderService` sends through the configured `IEmailProvider`
9. the handler updates `EmailLog.Status`, `SentDate`, `ProviderMessageId`, and `ErrorMessage`

### Retry behavior

Email jobs are currently created with:

- `MaxRetries = 1`

In this background framework, that means a single delivery attempt with no automatic retry after failure.

This is deliberate. It reduces the risk of duplicate emails if the provider accepts the message but the process fails before the database update completes.

Operationally, failed emails are expected to be retried manually from the admin email log.

### Duplicate-send guard

`SendEmailTaskHandler` checks whether the linked `EmailLog` is already marked `Sent` with a populated `SentDate`. If so, it skips the send attempt.

This provides a limited safety guard against re-processing the same job, but it is not a full external-provider idempotency mechanism.

## Data Model

The persisted email audit record is `EmailLog`.

```csharp
public class EmailLog : DatabaseEntityObject
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public EmailLogStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public string? TemplateName { get; set; }
}
```

### Field meanings

| Field | Meaning |
|---|---|
| `FromAddress` / `FromName` | Sender shown on the message |
| `ToAddress` / `ToName` | Single destination currently tracked by the system |
| `Subject` | Final rendered subject |
| `Content` | Final rendered body |
| `IsHtml` | Whether content should be treated as HTML |
| `Status` | `Queued`, `Sent`, or `Failed` |
| `ErrorMessage` | Failure reason if sending failed |
| `ProviderMessageId` | Provider response identifier if available |
| `ScheduledDate` | Intended send time used for queue monitoring |
| `SentDate` | Time the system marked the email as sent |
| `OpenedDate` | Reserved field, not currently populated by a tracking pipeline |
| `TemplateName` | Logical template used to render the email |

## Built-In Usage Paths

### 1. Identity email flows

`IdentityEmailSender` is registered as `IEmailSender<ApplicationUser>` and is used by account flows for:

- email confirmation
- password reset links
- password reset codes

It:

- renders the template
- sets sender values from configuration
- queues the email through `IBackgroundWorker`

### 2. Admin-created invited users

`UserProfileService.CreateUserAsync` creates a user without a password and then calls `SendRegistrationEmailAsync`.

That registration flow now:

- generates an email confirmation token
- base64url-encodes it
- builds an absolute `/account/confirm-email` link from the current HTTP request
- renders the `Registration` email template
- queues the email as a background task

When the recipient confirms email through the link, the confirm-email page can prompt the user to set a password if the account does not yet have one.

## How To Use It In New Code

### Option 1: Use the existing identity sender

If you are working inside an identity/account flow, prefer the existing `IEmailSender<ApplicationUser>` registration.

That path already handles:

- template rendering
- sender defaults
- queue submission
- email logging

### Option 2: Queue an application email directly

If you need a non-identity email, create an `EmailRequest` and submit it through `IBackgroundWorker`.

Example shape:

```csharp
var emailRequest = new EmailRequest
{
    From = new EmailAddress { Name = "DevCoreApp", Address = "noreply@example.com" },
    To = [new EmailAddress { Name = "User", Address = "user@example.com" }],
    Subject = "Subject",
    Content = "<p>Hello</p>",
    IsHtml = true,
    TemplateName = "MyTemplate"
};

backgroundWorker.Submit(new BackgroundRequestItem
{
    RequestType = BackgroundRequestType.SendEmail,
    Content = emailRequest
});
```

Notes:

- only one `To` recipient is supported
- you do not need to create `EmailLog` manually
- the background worker will create the `EmailLog` row if `EmailLogId` is empty

### Option 3: Add a new template

To add a new templated email:

1. add a new HTML or text file under `src/Server/Admin/WebService/wwwroot/email-templates`
2. register it in `EmailTemplateRepository`
3. render it through `IEmailTemplateService`
4. queue the resulting content with `IBackgroundWorker`

## Admin UI

The admin email log page is:

```text
/admin/email-log
```

The detail page is:

```text
/admin/email-log/{id}
```

### Capabilities

The admin UI supports:

- search
- filter by status
- filter by template
- date filters
- grouping by status
- previewing HTML or text email content
- viewing the related background job
- deleting log entries
- resending failed emails
- bulk delete
- resend-all-failed

### Permissions

The pages require:

- `System.EmailLog.View`

Resend actions are additionally gated by:

- `System.EmailLog.Resend`

## Health Monitoring

`StuckEmailsHealthCheck` is registered under the ready endpoint and reports queued emails older than 30 minutes.

It now evaluates based on:

- `EmailLog.Status == Queued`
- `EmailLog.ScheduledDate < now - 30 minutes`

This matters because manually re-queued older emails update `ScheduledDate`, so they are not immediately reported as stuck.

## Operational Notes

### Sender address behavior is inconsistent

Identity-driven emails use:

- `EmailConfiguration:FromEmail`
- `EmailConfiguration:FromName`

But `UserProfileService.SendRegistrationEmailAsync` still hardcodes:

- `noreply@example.com`
- `DevCoreApp`

So invited-user registration emails do not currently follow the configured sender values.

### Background processing is required

Queued emails are only delivered if the background worker is running correctly in the web host. If the worker is down, emails will remain in `Queued`.

### Email log is the main operational tool

Because automatic retries are disabled for email jobs, the email log is the primary place to:

- detect failures
- inspect provider response ids
- resend failed messages

## Current Limitations

The most important implementation limits in the current starter are:

### 1. No full idempotency guarantee

The system now avoids automatic retries and skips already-sent logs, but it still does not use provider-level idempotency keys. A failure at exactly the wrong point can still leave ambiguity around whether the provider accepted the message.

### 2. Single-recipient only

The model and service layer now intentionally enforce one destination per email request.

### 3. SendGrid is not usable yet

The SendGrid provider is included as a starter placeholder only. If a project wants to use SendGrid, `SendGridEmailProvider` still needs to be implemented.

### 4. Configuration key mismatch risk

Provider setup expects `UserName`, while the checked-in sample config uses `Username`.

### 5. Registration sender values are hardcoded

Invited-user registration emails do not yet use the configured `FromEmail` and `FromName`.

### 6. No delivery webhooks or open tracking

`OpenedDate` exists in the model, but there is no implemented tracking pipeline that updates it.

## Recommended Developer Setup

For a new project using this starter:

1. configure a real SMTP account with `EmailConfiguration:SmtpServer`, `Port`, `UserName`, and `Password`
2. set `FromEmail` and `FromName` for your application brand
3. verify that the active provider in `Program.cs` is the one you want
4. test the account confirmation flow end to end
5. test a password reset email
6. verify the email log page records `Queued`, `Sent`, and `Failed` states correctly
7. decide whether to keep MailKit, switch to SMTP, or implement the SendGrid placeholder for your project
8. decide whether registration emails should also use configured sender values

## Summary

The email system is usable today for basic application email delivery, especially account confirmation and password reset flows. Its main strengths are:

- simple provider abstraction
- background delivery
- built-in templates
- delivery logging and resend tools

Its main tradeoffs are:

- no provider-level idempotency
- no multi-recipient support
- incomplete alternative-provider support
- a few configuration and consistency gaps that should usually be cleaned up early in a real project
