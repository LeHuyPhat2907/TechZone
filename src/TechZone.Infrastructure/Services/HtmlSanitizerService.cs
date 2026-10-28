using System.Net;
using System.Text.RegularExpressions;
using TechZone.Application.Common.Interfaces;

namespace TechZone.Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    // Regex matching dangerous script tags, objects, embeds, iframes, and styles
    private static readonly Regex DangerousTagsRegex = new(
        @"<\s*(script|object|embed|iframe|applet|meta|link|style)[^>]*?>[\s\S]*?<\s*/\s*\1\s*>|<\s*(script|object|embed|iframe|applet|meta|link|style)[^>]*?/>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex matching inline event handlers (onload, onerror, onclick, onmouseover...)
    private static readonly Regex EventHandlersRegex = new(
        @"\bon\w+\s*=\s*(?:'[^']*'|""[^""]*""|[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex matching javascript: or vbscript: or data: in href/src
    private static readonly Regex JavascriptProtocolRegex = new(
        @"(href|src)\s*=\s*(['""]?)\s*(javascript|vbscript|data):",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex matching all HTML tags for pure plain-text stripping
    private static readonly Regex AllHtmlTagsRegex = new(
        @"<[^>]*>",
        RegexOptions.Compiled);

    public string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sanitized = input;
        sanitized = DangerousTagsRegex.Replace(sanitized, string.Empty);
        sanitized = EventHandlersRegex.Replace(sanitized, string.Empty);
        sanitized = JavascriptProtocolRegex.Replace(sanitized, "$1=$2unsafe-protocol:");

        return sanitized.Trim();
    }

    public string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var stripped = AllHtmlTagsRegex.Replace(input, string.Empty);
        return WebUtility.HtmlDecode(stripped).Trim();
    }
}
