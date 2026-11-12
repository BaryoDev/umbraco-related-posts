namespace BaryoDev.Umbraco.RelatedPosts;

internal static class SummaryText
{
    /// <summary>Cuts to <paramref name="max"/> characters at a word edge and adds "..." when text was dropped. A max of 0 or less keeps the text.</summary>
    public static string? Cut(string? text, int max)
    {
        if (text is null || max <= 0 || text.Length <= max)
        {
            return text;
        }
        var end = max;
        if (char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }
        var space = text.LastIndexOf(' ', end - 1, end);
        if (space > max / 2)
        {
            end = space;
        }
        return text[..end].TrimEnd(' ', ',', ';', ':', '.', '-') + "...";
    }
}
