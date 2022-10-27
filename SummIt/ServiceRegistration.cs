using System.Text.RegularExpressions;
using JetBrains.Space.Common;
using Microsoft.EntityFrameworkCore;
using SummIt.DB;
using SummIt.Handlers;
using SummIt.Services.Space;
using SummIt.Services.Summarize;

namespace SummIt;

public static class ServiceRegistration
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<ISpaceClientProvider, SpaceClientProvider>();

        builder.Services.AddSingleton<Func<AppInstallation, Connection>>(
            serviceProvider =>
                appInstallation => new ClientCredentialsConnection(
                    new Uri(appInstallation.ServerUrl),
                    appInstallation.ClientId,
                    appInstallation.ClientSecret,
                    serviceProvider.GetService<IHttpClientFactory>().CreateClient()
                )
        );

        builder.Services.AddSingleton<IChatMessageService, ChatMessageService>();

        builder.Services.AddSingleton<IPermissionRequestService, PermissionRequestService>();

        builder.Services.AddSingleton<IContextService, ContextService>();

        builder.Services.AddSingleton<IRepositorySummarizingService, RepositorySummarizingService>();
        builder.Services.AddSingleton<IChannelSummarizingService, ChannelSummarizingService>();

        builder.Services.AddSingleton<ITextService, TextService>();

        builder.Services.AddSingleton<IWordCloudService, WordCloudService>();

        builder.Services.AddSpaceWebHookHandler<WebhookHandler>();

        builder.Services.AddSingleton<IAppInstallationStore, AppInstallationStore>();
        builder.Services.AddDbContextFactory<SummItAppContext>(options => options.UseNpgsql(GetConnectionString()));

        return builder;
    }

    private static string GetConnectionString()
    {
        var connectionSting = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionSting))
        {
            return connectionSting;
        }

        var m = Regex.Match(Environment.GetEnvironmentVariable("DATABASE_URL")!, @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
        return $"Server={m.Groups[3]};Port={m.Groups[4]};User Id={m.Groups[1]};Password={m.Groups[2]};Database={m.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
    }
}