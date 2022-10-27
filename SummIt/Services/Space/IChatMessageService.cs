using JetBrains.Space.Client;
using SummIt.Models;

namespace SummIt.Services.Space;

public interface IChatMessageService
{
    Task SendHelpMessageAsync(string clientId, string recipientUserId, Commands commands);
    Task SendMessageAsync(string clientId, string recipientUserId, string message);
    Task SendRequestPermissionsMessageAsync(string clientId, string recipientUserId);
    Task SendWordsCloudAsync(string clientId, string recipientUserId, string attachmentId);
}