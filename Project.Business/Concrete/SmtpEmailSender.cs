using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.Business.Abstract;
using Project.Business.Settings;

namespace Project.Business.Concrete;

// WARNING: Bu SMTP yapılandırması (Gmail) sadece LOKAL GELİŞTİRME ortamı içindir.
// Canlıya (Production) çıkarken SendGrid, Postmark veya AWS SES gibi profesyonel bir 
// e-posta sağlayıcısına geçilmeli ve kimlik bilgileri sunucu üzerindeki 
// Environment Variables (Ortam Değişkenleri) aracılığıyla beslenmelidir!
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection(SmtpSettings.SectionName).Get<SmtpSettings>()
            ?? new SmtpSettings();
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            throw new InvalidOperationException("SmtpSettings:Host yapılandırılmamış.");
        }

        var fromAddress = string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? _settings.Username
            : _settings.FromEmail;

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("SmtpSettings:FromEmail veya Username yapılandırılmamış.");
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromAddress, _settings.FromName),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };
        mailMessage.To.Add(new MailAddress(toEmail));

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("SMTP e-posta gönderildi: {ToEmail} | {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP e-posta gönderilemedi: {ToEmail} | {Subject}", toEmail, subject);
            throw;
        }
    }
}
