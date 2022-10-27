using System.Collections.Concurrent;
using System.Web;
using CliWrap;
using JetBrains.Space.Client;
using Microsoft.Extensions.Caching.Memory;
using SummIt.Extensions;
using SummIt.Services.Space;

namespace SummIt.Services.Summarize;

public class ChannelSummarizingService : IChannelSummarizingService
{
    private readonly ISpaceClientProvider _spaceClientProvider;
    private readonly ITextService _textService;

    private const int BatchSize = 50;
    private const int MaxMessagesToScan = 1024;

    public ChannelSummarizingService(ISpaceClientProvider spaceClientProvider, ITextService textService)
    {
        _spaceClientProvider = spaceClientProvider;
        _textService = textService;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetChannelKeywordsAsync(
        string clientId,
        ChannelIdentifier channelIdentifier
    )
    {
        var chatClient = await _spaceClientProvider.GetChatClientAsync(clientId);

        var scannedCount = 0;
        var hasMore = true;
        DateTime? startFromDate = null;
        var histogram = new ConcurrentDictionary<string, int>();
        while (hasMore && scannedCount < MaxMessagesToScan)
        {
            var messagesResponse = await chatClient.Messages.GetChannelMessagesAsync(
                channelIdentifier,
                MessagesSorting.FromNewestToOldest,
                BatchSize,
                startFromDate,
                partial => partial.AddFieldNames(new[] { "messages(text)", "nextStartFromDate" })
            );

            scannedCount += messagesResponse.Messages.Count;
            ScanMessages(messagesResponse.Messages, histogram);

            if (messagesResponse.Messages.Count > 0 && messagesResponse.NextStartFromDate.HasValue)
            {
                startFromDate = messagesResponse.NextStartFromDate.Value;
            }
            else
            {
                hasMore = false;
            }
        }

        return histogram;
    }

    private void ScanMessages(IEnumerable<ChannelItemRecord> messages, ConcurrentDictionary<string, int> histogram)
    {
        foreach (var message in messages)
        {
            foreach (var token in _textService.TokenizeText(message.Text))
            {
                histogram.Increase(token);
            }
        }
    }
}