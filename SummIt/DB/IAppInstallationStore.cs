namespace SummIt.DB;

public interface IAppInstallationStore
{
    Task RegisterAppInstallationAsync(AppInstallation appInstallation);
    Task<AppInstallation> GetAppInstallationAsync(string clientId);
}
