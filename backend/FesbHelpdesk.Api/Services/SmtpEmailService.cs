using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FesbHelpdesk.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        _host = config["Smtp:Host"] ?? throw new InvalidOperationException("SMTP host not configured");
        _port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        _username = config["Smtp:Username"] ?? string.Empty;
        _password = config["Smtp:Password"] ?? string.Empty;
        _fromEmail = config["Smtp:FromEmail"] ?? _username;
        _fromName = config["Smtp:FromName"] ?? "FESB Helpdesk";
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SMTP send skipped: empty recipient");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var socketOption = _port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(_host, _port, socketOption);

            if (!string.IsNullOrWhiteSpace(_username))
            {
                await smtp.AuthenticateAsync(_username, _password);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", toEmail);
        }
    }
}
