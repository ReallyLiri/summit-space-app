using System.ComponentModel;
using SummIt.Extensions;
using JetBrains.Annotations;
using JetBrains.Space.AspNetCore.Experimental.WebHooks;
using JetBrains.Space.Client;
using JetBrains.Space.Common;
using SummIt.DB;
using SummIt.Models;
using SummIt.Models.Attributes;
using SummIt.Services.Space;
using SummIt.Services.Summarize;
using Commands = JetBrains.Space.Client.Commands;

namespace SummIt.Handlers;

[UsedImplicitly]
public class WebhookHandler : SpaceWebHookHandler
{
    private readonly ILogger<WebhookHandler> _logger;
    private readonly IAppInstallationStore _appInstallationStore;
    private readonly IChatMessageService _chatMessageService;
    private readonly IPermissionRequestService _permissionRequestService;
    private readonly IContextService _contextService;
    private readonly IRepositorySummarizingService _repositorySummarizingService;
    private readonly IChannelSummarizingService _channelSummarizingService;
    private readonly IWordCloudService _wordCloudService;
    private readonly ISpaceClientProvider _spaceClientProvider;

    private const int MinimalTokensCount = 10;
    private const int TargetTokensCount = 64;

    private static readonly string RepositoryCommandPrefix = SummItCommands.Repository.GetText<CommandNameAttribute>(_ => _.Name);
    private static readonly string ChannelCommandPrefix = SummItCommands.Channel.GetText<CommandNameAttribute>(_ => _.Name);

    public WebhookHandler(
        ILogger<WebhookHandler> logger,
        IAppInstallationStore appInstallationStore,
        IChatMessageService chatMessageService,
        IPermissionRequestService permissionRequestService,
        IContextService contextService,
        IRepositorySummarizingService repositorySummarizingService,
        IChannelSummarizingService channelSummarizingService,
        IWordCloudService wordCloudService,
        ISpaceClientProvider spaceClientProvider
    )
    {
        _logger = logger;
        _appInstallationStore = appInstallationStore;
        _chatMessageService = chatMessageService;
        _permissionRequestService = permissionRequestService;
        _contextService = contextService;
        _repositorySummarizingService = repositorySummarizingService;
        _channelSummarizingService = channelSummarizingService;
        _wordCloudService = wordCloudService;
        _spaceClientProvider = spaceClientProvider;
    }

    public override async Task<ApplicationExecutionResult> HandleInitAsync(InitPayload payload)
    {
        _logger.LogInformation($"Received {nameof(InitPayload)} event for client-id '{payload.ClientId}'");
        await _appInstallationStore.RegisterAppInstallationAsync(new AppInstallation(
            payload.ServerUrl,
            payload.ClientId,
            payload.ClientSecret
        ));
        await _permissionRequestService.RequestPermissionsAsync(payload.ClientId);
        return await base.HandleInitAsync(payload);
    }

    public override async Task<Commands> HandleListCommandsAsync(ListCommandsPayload payload) =>
        new(
            Enum.GetValues<SummItCommands>()
                .Select(command => new CommandDetail(
                    command.GetText<CommandNameAttribute>(_ => _.Name),
                    command.GetText<DescriptionAttribute>(_ => _.Description))
                )
                .ToList()
        );

    public override async Task HandleMessageAsync(MessagePayload payload)
    {
        try
        {
            _logger.LogInformation($"Received {nameof(MessagePayload)} for client-id '{payload.ClientId}'");
            var messageText = payload.Message.Body as ChatMessageText;
            if (string.IsNullOrEmpty(messageText?.Text))
            {
                _logger.LogInformation("Message is empty");
                return;
            }

            var fullText = messageText.Text.Trim().TrimStart('/');
            if (fullText.StartsWith(RepositoryCommandPrefix))
            {
                var query = fullText[RepositoryCommandPrefix.Length..].Trim();
                await SummarizeRepositoryAsync(payload, query);
                return;
            }

            if (fullText.StartsWith(ChannelCommandPrefix))
            {
                var query = fullText[ChannelCommandPrefix.Length..].Trim();
                await SummarizeChannelAsync(payload, query);
                return;
            }

            await _chatMessageService.SendHelpMessageAsync(
                payload.ClientId,
                payload.UserId,
                await HandleListCommandsAsync(new ListCommandsPayload { UserId = payload.UserId })
            );
        }
        catch (PermissionDeniedException permissionDeniedException)
        {
            _logger.LogInformation(permissionDeniedException, "Missing permissions");
            await _chatMessageService.SendRequestPermissionsMessageAsync(payload.ClientId, payload.UserId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Something went wrong");
            await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Something went wrong {SpaceBuiltinEmojis.Sad}");
        }
    }

    private async Task SummarizeRepositoryAsync(MessagePayload payload, string query)
    {
        var (project, repository, message) = await _contextService.SearchRepositoryAsync(payload.ClientId, query);
        if (!string.IsNullOrEmpty(message))
        {
            await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, message);
            return;
        }

        await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Summarizing repository... {SpaceBuiltinEmojis.InProgress}");

        DispatchSummarize(
            payload, query, "repository",
            async () => await _repositorySummarizingService.GetRepositoryKeywordsAsync(payload.ClientId, project, repository)
        );
    }

    private async Task SummarizeChannelAsync(MessagePayload payload, string query)
    {
        var (channel, message) = await _contextService.SearchChannelAsync(payload.ClientId, query);
        if (!string.IsNullOrEmpty(message))
        {
            await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, message);
            return;
        }

        await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Summarizing channel... {SpaceBuiltinEmojis.InProgress}");

        DispatchSummarize(
            payload, query, "channel",
            async () => await _channelSummarizingService.GetChannelKeywordsAsync(payload.ClientId, channel)
        );
    }

    private void DispatchSummarize(MessagePayload payload, string query, string type, Func<Task<IReadOnlyDictionary<string, int>>> summarizer) =>
        Task.Run(async () =>
        {
            try
            {
                var histogram = await summarizer();
                await HandleHistogramAsync(payload, type, query, histogram);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Summarizing {type} failed");
                await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Something went wrong {SpaceBuiltinEmojis.Sad}");
                throw;
            }
        });

    private async Task HandleHistogramAsync(MessagePayload payload, string type, string query, IReadOnlyDictionary<string, int> histogram)
    {
        if (histogram == null)
        {
            await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Failed to summarize {type} {SpaceBuiltinEmojis.Conflict}");
            return;
        }

        _logger.LogInformation($"Generated histogram with {histogram.Count} entries");

        if (histogram.Count < MinimalTokensCount)
        {
            await _chatMessageService.SendMessageAsync(payload.ClientId, payload.UserId, $"Not enough data was found for {type} {SpaceBuiltinEmojis.Sad}");
            return;
        }

        var uploadClient = await _spaceClientProvider.GetUploadClientAsync(payload.ClientId);
        var targetFileName = Path.GetInvalidFileNameChars().Aggregate(query, (str, c) => str.Replace(c, '_'));
        var attachmentId = await _wordCloudService.CreateWordCloudAsync(histogram.Top(TargetTokensCount), async stream
            => await uploadClient.UploadImageAsync(
                "attachments",
                $"{targetFileName}.png",
                stream
            ));
        await _chatMessageService.SendWordsCloudAsync(payload.ClientId, payload.UserId, attachmentId);
    }
}