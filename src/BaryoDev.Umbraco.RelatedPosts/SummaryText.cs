using System.Text.RegularExpressions;

namespace BaryoDev.Umbraco.RelatedPosts;

internal static partial class SummaryText
{
    [GeneratedRegex("<[^>]*>", RegexOptions.None, 250)]
    private static partial Regex Tags();

    [GeneratedRegex("\\s+", RegexOptions.None, 250)]
    private static partial Regex Spaces();

    /// <summary>
    /// Strips tags, then cuts to <paramref name="max"/> characters at a word edge and adds "..." when text was dropped,
    /// so rich text is never cut inside a tag. A max of 0 or less keeps the length.
    /// </summary>
    public static string? Cut(string? text, int max)
    {
        if (text is null)
        {
            return null;
        }
        try
        {
            text = Spaces().Replace(Tags().Replace(text, " "), " ").Trim();
        }
        catch (RegexMatchTimeoutException)
        {
            return string.Empty;
        }
        if (max <= 0 || text.Length <= max)
        {
            return text;
        }
        var end = max;
        if (end > 0 && char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }
        if (end > 0)
        {
            var space = text.LastIndexOf(' ', end - 1, end);
            if (space > max / 2)
            {
                end = space;
            }
        }
        return text[..end].TrimEnd(' ', ',', ';', ':', '.', '-') + "...";
    }
}
