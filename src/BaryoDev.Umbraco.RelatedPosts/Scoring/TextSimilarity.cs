using System.Text.RegularExpressions;

namespace BaryoDev.Umbraco.RelatedPosts.Scoring;

/// <summary>TF-IDF cosine similarity over a small candidate set.</summary>
internal static partial class TextSimilarity
{
    private static readonly HashSet<string> Stop = new(StringComparer.Ordinal)
    {
        "the","and","for","are","but","not","you","all","can","had","her","was","one","our","out","has","have",
        "from","they","this","that","with","will","your","what","when","which","their","there","been","were",
        "into","than","then","them","these","those","about","would","could","should","also","more","some","such"
    };

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Word();

    public static string[] Tokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return Word().Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(w => w.Length >= 3 && !Stop.Contains(w))
            .ToArray();
    }

    /// <summary>Returns one cosine value in 0..1 per document against the query.</summary>
    public static double[] Score(string[] query, IReadOnlyList<string[]> docs)
    {
        var result = new double[docs.Count];
        if (query.Length == 0 || docs.Count == 0) return result;

        var df = new Dictionary<string, int>();
        foreach (var d in docs.Append(query))
            foreach (var t in d.Distinct()) df[t] = df.GetValueOrDefault(t) + 1;
        var n = docs.Count + 1;

        Dictionary<string, double> Vector(string[] tokens) =>
            tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count() * (Math.Log((1.0 + n) / (1.0 + df[g.Key])) + 1));

        var q = Vector(query);
        var qNorm = Math.Sqrt(q.Values.Sum(v => v * v));
        for (var i = 0; i < docs.Count; i++)
        {
            if (docs[i].Length == 0) continue;
            var v = Vector(docs[i]);
            var dot = q.Where(kv => v.ContainsKey(kv.Key)).Sum(kv => kv.Value * v[kv.Key]);
            var vNorm = Math.Sqrt(v.Values.Sum(x => x * x));
            if (dot > 0 && qNorm > 0 && vNorm > 0) result[i] = dot / (qNorm * vNorm);
        }
        return result;
    }
}
