using Microsoft.Extensions.Logging;

namespace Gateway.Api.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Simulación de email: To={To}, Subject={Subject}, Body={Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

