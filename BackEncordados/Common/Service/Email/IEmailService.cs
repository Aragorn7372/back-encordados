namespace BackEncordados.Common.Service.Email;


public class EmailMessage
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; } = true;
}


public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message);

    Task EnqueueEmailAsync(EmailMessage message);
}