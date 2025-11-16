using Microsoft.Extensions.Options;
using stockmind.Utils;
using System.Net;
using System.Net.Mail;

namespace stockmind.Services;

public class EmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendEmailAsync(string recipient, string subject, string body, bool isBodyHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient email is required.", nameof(recipient));
        }

        return SendEmailAsync(new[] { recipient }, subject, body, isBodyHtml, cancellationToken);
    }

    public async Task SendEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml = false, CancellationToken cancellationToken = default)
    {
        var recipientList = recipients?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                            ?? throw new ArgumentNullException(nameof(recipients));
        if (recipientList.Count == 0)
        {
            throw new ArgumentException("At least one recipient must be provided.", nameof(recipients));
        }

        cancellationToken.ThrowIfCancellationRequested();
        ValidateOptions();

        using var message = BuildMailMessage(recipientList, subject, body, isBodyHtml);
        using var client = BuildSmtpClient();

        try
        {
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Email sent to {Recipients}", string.Join(", ", recipientList));
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", recipientList));
            throw;
        }
    }

    private MailMessage BuildMailMessage(List<string> recipients, string subject, string body, bool isBodyHtml)
    {
        var from = DetermineFromAddress();
        var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject ?? string.Empty,
            Body = body ?? string.Empty,
            IsBodyHtml = isBodyHtml
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(new MailAddress(recipient));
        }

        return message;
    }

    private SmtpClient BuildSmtpClient()
    {
        var host = _options.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Email:Host configuration is required.");
        }

        var port = _options.Port > 0 ? _options.Port.Value : 25;
        var client = new SmtpClient(host, port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (_options.TimeoutSeconds.HasValue && _options.TimeoutSeconds > 0)
        {
            client.Timeout = (int)TimeSpan.FromSeconds(_options.TimeoutSeconds.Value).TotalMilliseconds;
        }

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.From) && string.IsNullOrWhiteSpace(_options.Username))
        {
            throw new InvalidOperationException("Email:From or Email:Username must be configured.");
        }
    }

    private string DetermineFromAddress()
    {
        if (!string.IsNullOrWhiteSpace(_options.From))
        {
            return _options.From;
        }

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            return _options.Username;
        }

        throw new InvalidOperationException("Unable to determine a From address. Configure Email:From or Email:Username.");
    }
}
