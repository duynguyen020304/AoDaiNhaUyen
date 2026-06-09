namespace AoDaiNhaUyen.Application.Interfaces.Services;

public sealed record RenderedEmail(string Subject, string HtmlBody, string? TextBody);

public interface IEmailTemplateService
{
  Task<RenderedEmail> RenderAsync(
    string templateKey,
    string payloadJson,
    string locale = "vi-VN",
    CancellationToken cancellationToken = default);
}
