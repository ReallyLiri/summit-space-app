namespace SummIt.Services.Summarize;

public interface IWordCloudService
{
    Task<T> CreateWordCloudAsync<T>(IReadOnlyDictionary<string, int> histogram, Func<Stream, Task<T>> streamConsumer);
}