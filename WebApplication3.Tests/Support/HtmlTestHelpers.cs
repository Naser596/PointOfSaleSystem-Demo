using System.Text.RegularExpressions;

namespace WebApplication3.Tests.Support;

public static partial class HtmlTestHelpers
{
    public static string ExtractRequestVerificationToken(string html)
    {
        var match = RequestVerificationTokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Anti-forgery token was not found in the HTML.");
        }

        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }

    [GeneratedRegex("value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"|name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex RequestVerificationTokenRegex();
}
