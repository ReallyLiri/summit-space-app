using JetBrains.Annotations;
using JetBrains.Space.Common;

namespace SummIt.Services.Space;

public class TokenProvidingClientCredentialsConnection : ClientCredentialsConnection
{
    public TokenProvidingClientCredentialsConnection(
        [NotNull] Uri serverUrl, 
        [NotNull] string clientId,
        [NotNull] string clientSecret, 
        [CanBeNull] HttpClient httpClient = null
    ) : base(serverUrl, clientId, clientSecret, httpClient)
    {
    }

    public async Task<string> GetBearerTokenAsync()
    {
        using var request = new HttpRequestMessage();
        await EnsureAuthenticatedAsync(request, default);
        return request.Headers.Authorization?.Parameter;
    }
}