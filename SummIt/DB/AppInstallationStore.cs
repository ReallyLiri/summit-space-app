using Microsoft.EntityFrameworkCore;

namespace SummIt.DB;

public class AppInstallationStore : IAppInstallationStore
{
    private readonly IDbContextFactory<SummItAppContext> _appContextFactory;

    public AppInstallationStore(IDbContextFactory<SummItAppContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task RegisterAppInstallationAsync(AppInstallation appInstallation)
    {
        await using var appContext = await _appContextFactory.CreateDbContextAsync();
        await appContext.UpsertAsync(appInstallation);
    }

    public async Task<AppInstallation> GetAppInstallationAsync(string clientId)
    {
        await using var appContext = await _appContextFactory.CreateDbContextAsync();
        return await appContext.GetAsync(clientId);
    }
}