using Humanizer;

namespace SummIt.Services.Summarize;

public class TextService : ITextService
{
    private static readonly ISet<char> NoiseCharacters = new HashSet<char>
    {
        '(', ')', '{', '}', '[', ']', '-', '_', '+', '=', '\\', '/', '"', '\'',
        ',', ';', '.', '?', '!', '&', '%', '~', '`'
    };

    public IEnumerable<string> TokenizeName(string name)
        => string.Join("", name.Humanize().Where(_ => !NoiseCharacters.Contains(_)))
            .Split(" ")
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Where(_ => !_.StartsWith('@') && !_.StartsWith('#'))
            .Select(_ => _.ToLowerInvariant())
            .Where(_ => !CommonWords.Contains(_));

    public IEnumerable<string> TokenizeText(string text)
        => text.Split(null).SelectMany(TokenizeName);

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