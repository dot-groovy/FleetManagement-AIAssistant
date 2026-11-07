using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace GestaoFrota.Services;
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    // Construtor - recebe as configurações de email do appsettings.json
    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // Envia um email com PDF em anexo através do servidor SMTP (MailHog/smtp4dev)
    public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string fileName)
    {
        var remetente = _config["EmailSettings:Email"] ?? "noreply@gestaofrota.local";
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(remetente));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        var builder = new BodyBuilder { HtmlBody = body };
        builder.Attachments.Add(fileName, attachment, new ContentType("application", "pdf"));
        email.Body = builder.ToMessageBody();
        using var smtp = new SmtpClient();
        var host = _config["EmailSettings:Host"] ?? "localhost";
        var port = int.Parse(_config["EmailSettings:Port"] ?? "1025");
        var password = _config["EmailSettings:Password"];
        if (string.IsNullOrEmpty(password))
        {
            await smtp.ConnectAsync(host, port, SecureSocketOptions.None);
        }
        else
        {
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(remetente, password);
        }
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
