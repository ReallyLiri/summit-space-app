using JetBrains.Space.Client;

namespace SummIt.Services.Summarize;

public interface IChannelSummarizingService
{
    Task<IReadOnlyDictionary<string, int>> GetChannelKeywordsAsync(
        string clientId,
        ChannelIdentifier channelIdentifier
    );
}