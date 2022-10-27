using JetBrains.Space.Client;

namespace SummIt.Services.Space;

public interface ISpaceClientProvider
{
    Task<ChatClient> GetChatClientAsync(string clientId);
    Task<ApplicationClient> GetApplicationClientAsync(string clientId);
    Task<ProjectClient> GetProjectClientAsync(string clientId);
    Task<UploadClient> GetUploadClientAsync(string clientId);
}