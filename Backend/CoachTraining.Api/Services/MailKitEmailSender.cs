using CoachTraining.Api.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CoachTraining.Api.Services;

public class MailKitEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly IWebHostEnvironment _environment;

    public MailKitEmailSender(IOptions<SmtpSettings> settings, IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _environment = environment;
    }

    public async Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplate", "PasswordReset.html");
        var html = await File.ReadAllTextAsync(templatePath, cancellationToken);
        html = html.Replace("{{fullName}}", System.Net.WebUtility.HtmlEncode(recipientName), StringComparison.Ordinal)
            .Replace("{{resetUrl}}", System.Net.WebUtility.HtmlEncode(resetUrl), StringComparison.Ordinal)
            .Replace("{{expiryMinutes}}", "10", StringComparison.Ordinal);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "ตั้งรหัสผ่านใหม่สำหรับระบบบริหารการฝึกซ้อม";
        message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
