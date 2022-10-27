using JetBrains.Space.Client;
using JetBrains.Space.Common;
using SummIt.DB;

namespace SummIt.Services.Space;

public class SpaceClientProvider : ISpaceClientProvider
{
    private readonly IAppInstallationStore _appInstallationStore;
    private readonly Func<AppInstallation, Connection> _connectionBuilder;

    public SpaceClientProvider(IAppInstallationStore appInstallationStore, Func<AppInstallation, Connection> connectionBuilder)
    {
        _appInstallationStore = appInstallationStore;
        _connectionBuilder = connectionBuilder;
    }

    public async Task<ChatClient> GetChatClientAsync(string clientId) => new(await GetConnectionAsync(clientId));

    public async Task<ApplicationClient> GetApplicationClientAsync(string clientId) => new(await GetConnectionAsync(clientId));

    public async Task<ProjectClient> GetProjectClientAsync(string clientId) => new(await GetConnectionAsync(clientId));

    public async Task<UploadClient> GetUploadClientAsync(string clientId) => new(await GetConnectionAsync(clientId));

    private async Task<Connection> GetConnectionAsync(string clientId)
    {
        if (clientId == null)
        {
            throw new ArgumentNullException(nameof(clientId));
        }

        var appInstallation = await _appInstallationStore.GetAppInstallationAsync(clientId);
        if (appInstallation is null)
        {
            throw new ApplicationException($"No app registration found for client-in '{clientId}'");
        }

        return _connectionBuilder(appInstallation);
    }
}