using JetBrains.Space.Client;

namespace SummIt.Services.Space;

public class PermissionRequestService : IPermissionRequestService
{
    private readonly ISpaceClientProvider _spaceClientProvider;
    private readonly List<string> _requestedPermissions;

    public PermissionRequestService(IConfiguration configuration, ISpaceClientProvider spaceClientProvider)
    {
        _spaceClientProvider = spaceClientProvider;
        _requestedPermissions = configuration.GetValue<string>("App:RequestedPermissions").Split(",").ToList();
    }

    public async Task RequestPermissionsAsync(string clientId)
    {
        var applicationClient = await _spaceClientProvider.GetApplicationClientAsync(clientId);
        await Task.WhenAll(
            applicationClient.Authorizations.AuthorizedRights.RequestRightsAsync(
                ApplicationIdentifier.Me,
                PermissionContextIdentifier.Global,
                _requestedPermissions
            ),
            applicationClient.SetUiExtensionsAsync(
                PermissionContextIdentifier.Global,
                new List<AppUiExtensionIn>
                {
                    new ChatBotUiExtensionIn()
                }
            )
        );
    }
}