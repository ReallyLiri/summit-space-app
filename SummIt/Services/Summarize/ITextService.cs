namespace SummIt.Services.Summarize;

public interface ITextService
{
    IEnumerable<string> TokenizeName(string name);
    IEnumerable<string> TokenizeText(string text);
}