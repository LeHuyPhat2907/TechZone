namespace TechZone.Application.Common.Interfaces;

public interface IHtmlSanitizerService
{
    string Sanitize(string? input);
    string StripHtml(string? input);
}
