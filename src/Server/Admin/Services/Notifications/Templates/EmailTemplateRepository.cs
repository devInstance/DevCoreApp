namespace DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;

public static class EmailTemplateRepository
{
    private static readonly Dictionary<string, EmailTemplateDescriptor> Templates = new()
    {
        [EmailTemplateName.Registration] = new EmailTemplateDescriptor(
            "Complete Your Registration",
            "email-templates/registration.html",
            true),

        [EmailTemplateName.ConfirmEmail] = new EmailTemplateDescriptor(
            "Confirm your email",
            "email-templates/confirm-email.html",
            true),

        [EmailTemplateName.PasswordResetLink] = new EmailTemplateDescriptor(
            "Reset your password",
            "email-templates/password-reset-link.html",
            true),

        [EmailTemplateName.PasswordResetCode] = new EmailTemplateDescriptor(
            "Reset your password",
            "email-templates/password-reset-code.html",
            false),
    };

    public static EmailTemplateDescriptor Get(string name)
    {
        if (Templates.TryGetValue(name, out var descriptor))
        {
            return descriptor;
        }

        throw new KeyNotFoundException($"Email template '{name}' is not registered.");
    }
}
