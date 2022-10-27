using JetBrains.Space.Client;
using SummIt.Extensions;
using SummIt.Models;

namespace SummIt.Services.Space;

public class ChatMessageService : IChatMessageService
{
    private readonly ILogger<ChatMessageService> _logger;
    private readonly ISpaceClientProvider _spaceClientProvider;

    public ChatMessageService(ILogger<ChatMessageService> logger, ISpaceClientProvider spaceClientProvider)
    {
        _logger = logger;
        _spaceClientProvider = spaceClientProvider;
    }

    public async Task SendHelpMessageAsync(string clientId, string recipientUserId, Commands commands)
    {
        _logger.LogInformation($"Sending help message for client-id '{clientId}' and user '{recipientUserId}'");
        await SendMessageAsync(
            clientId,
            recipientUserId,
            BlockMessage(
                MessageSectionElement.MessageSection(
                    header: "List of available commands",
                    elements: new List<MessageBlockElement>
                    {
                        MessageBlockElement.MessageFields(
                            commands.CommandsItems
                                .Select(it => MessageFieldElement.MessageField(it.Name, it.Description))
                                .ToList<MessageFieldElement>())
                    }),
                new MessageOutline("SummIt Bot")
            )
        );
    }

    public async Task SendMessageAsync(string clientId, string recipientUserId, string message) =>
        await SendMessageAsync(
            clientId,
            recipientUserId,
            ChatMessage.Text(message)
        );

    public async Task SendRequestPermissionsMessageAsync(string clientId, string recipientUserId) =>
        await SendMessageAsync(
            clientId,
            recipientUserId,
            BlockMessage(
                MessageSectionElement.MessageSection(
                    elements: new List<MessageBlockElement>
                    {
                        new MessageText($"Unfortunately, SummIt did not get the required permissions to perform the operation {SpaceBuiltinEmojis.Sad}"),
                    },
                    style: MessageStyle.SECONDARY
                )
            )
        );

    public async Task SendWordsCloudAsync(string clientId, string recipientUserId, string attachmentId) =>
        await SendMessageAsync(
            clientId,
            recipientUserId,
            ChatMessage.Text(SpaceBuiltinEmojis.Tada),
            new ImageAttachment(attachmentId, ImageDimensions.Width, ImageDimensions.Width)
        );

    private static ChatMessage BlockMessage(MessageSectionElement section, MessageOutlineBase outline = null) =>
        ChatMessage.Block(
            outline: outline,
            sections: new List<MessageSectionElement> { section }
        );

    private async Task SendMessageAsync(string clientId, string recipientUserId, ChatMessage message, AttachmentIn attachment = null)
    {
        var chatClient = await _spaceClientProvider.GetChatClientAsync(clientId);
        await chatClient.Messages.SendMessageAsync(
            recipient: MessageRecipient.Member(ProfileIdentifier.Id(recipientUserId)),
            content: message,
            attachments: attachment != null ? new List<AttachmentIn> { attachment } : null
        );
    }
}