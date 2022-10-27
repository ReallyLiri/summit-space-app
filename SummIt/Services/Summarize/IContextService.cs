using JetBrains.Space.Client;

namespace SummIt.Services.Summarize;

public interface IContextService
{
    Task<(ProjectIdentifier Project, string Repository, string Message)> SearchRepositoryAsync(string clientId, string query);
    Task<(ChannelIdentifier Channel, string Message)> SearchChannelAsync(string clientId, string query);
}