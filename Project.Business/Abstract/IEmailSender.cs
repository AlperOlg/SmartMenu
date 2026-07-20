namespace Project.Business.Abstract;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}
