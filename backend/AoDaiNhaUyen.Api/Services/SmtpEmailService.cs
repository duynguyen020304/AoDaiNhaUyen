using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AoDaiNhaUyen.Api.Services;

public sealed class SmtpEmailService(IOptions<EmailSettings> emailSettings) : IEmailService
{
  private readonly EmailSettings emailSettings = emailSettings.Value;

  public async Task SendEmailAsync(
    string toEmail,
    string subject,
    string htmlBody,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var message = new MimeMessage();
      message.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
      message.To.Add(MailboxAddress.Parse(toEmail));
      message.Subject = subject;
      message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

      using var client = new SmtpClient();
      var socketOptions = emailSettings.EnableSsl
        ? SecureSocketOptions.StartTls
        : SecureSocketOptions.None;

      await client.ConnectAsync(
        emailSettings.SmtpHost,
        emailSettings.SmtpPort,
        socketOptions,
        cancellationToken);
      await client.AuthenticateAsync(
        emailSettings.SmtpUsername,
        emailSettings.SmtpPassword.Replace(" ", string.Empty, StringComparison.Ordinal),
        cancellationToken);
      await client.SendAsync(message, cancellationToken);
      await client.DisconnectAsync(true, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to send email via SMTP.", ex);
    }
  }
}
