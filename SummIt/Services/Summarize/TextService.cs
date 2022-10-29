using Humanizer;

namespace SummIt.Services.Summarize;

public class TextService : ITextService
{
    public IEnumerable<string> TokenizeName(string name)
        => name.Humanize()
            .Split(null)
            .Where(_ => !_.StartsWith('@') && !_.StartsWith('#'))
            .Select(FilterOutChars)
            .Where(_ => _.Length >= 2)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Select(_ => _.ToLowerInvariant())
            .Where(_ => !CommonWords.Contains(_));

    public IEnumerable<string> TokenizeText(string text)
        => text.Split(null).SelectMany(TokenizeName);

    private static string FilterOutChars(string str)
        => string.Join("", str.Where(IsRelevantChar));

    private static bool IsRelevantChar(char ch) => char.IsLetter(ch);

    private static readonly ISet<string> CommonWords = new HashSet<string>
    {
        "the",
        "at",
        "there",
        "some",
        "my",
        "of",
        "be",
        "use",
        "her",
        "than",
        "and",
        "this",
        "an",
        "would",
        "first",
        "a",
        "have",
        "each",
        "make",
        "water",
        "to",
        "from",
        "which",
        "like",
        "been",
        "in",
        "or",
        "she",
        "him",
        "call",
        "is",
        "one",
        "do",
        "into",
        "who",
        "you",
        "had",
        "how",
        "time",
        "oil",
        "that",
        "by",
        "their",
        "has",
        "its",
        "it",
        "word",
        "if",
        "look",
        "now",
        "he",
        "but",
        "will",
        "two",
        "find",
        "was",
        "not",
        "up",
        "more",
        "long",
        "for",
        "what",
        "other",
        "write",
        "down",
        "on",
        "all",
        "about",
        "go",
        "day",
        "are",
        "were",
        "out",
        "see",
        "did",
        "as",
        "we",
        "many",
        "number",
        "get",
        "with",
        "when",
        "then",
        "no",
        "come",
        "his",
        "your",
        "them",
        "way",
        "made",
        "they",
        "can",
        "these",
        "could",
        "may",
        "I",
        "said",
        "so",
        "people",
        "part"
    };
}