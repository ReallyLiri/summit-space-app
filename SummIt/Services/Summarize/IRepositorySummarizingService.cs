using JetBrains.Space.Client;

namespace SummIt.Services.Summarize;

public interface IRepositorySummarizingService
{
    Task<IReadOnlyDictionary<string, int>> GetRepositoryKeywordsAsync(
        string clientId,
        ProjectIdentifier projectIdentifier,
        string repository
    );
}