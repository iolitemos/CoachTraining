namespace CoachTraining.Api.Services;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken = default);
}
