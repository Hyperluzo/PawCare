using PawCare.Api.Models;

namespace PawCare.Api.Services
{
    public record EmailTemplateResult(string Subject, string HtmlBody, string TextBody);

    public interface IEmailTemplateService
    {
        EmailTemplateResult BuildTemplate(string notificationType, Patient patient);
    }
}
