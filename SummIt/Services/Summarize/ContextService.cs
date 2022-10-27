using JetBrains.Space.Client;
using SummIt.Services.Space;

namespace SummIt.Services.Summarize;

public class ContextService : IContextService
{
    private readonly ISpaceClientProvider _spaceClientProvider;

    private const int DuplicatesLimit = 3;

    public ContextService(ISpaceClientProvider spaceClientProvider)
    {
        _spaceClientProvider = spaceClientProvider;
    }

    public async Task<(ProjectIdentifier Project, string Repository, string Message)> SearchRepositoryAsync(string clientId, string query)
    {
        var projectClient = await _spaceClientProvider.GetProjectClientAsync(clientId);

        var parts = query.Split("/");
        if (parts.Length > 2)
        {
            return (null, null, "Invalid search term (too many path separators)");
        }

        if (parts.Length > 1)
        {
            PRProject project;
            var projectQuery = parts[0].ToUpperInvariant();
            var projects = (await projectClient.GetAllProjectsAsync(top: DuplicatesLimit, term: projectQuery)).Data ?? new List<PRProject>();
            switch (projects.Count)
            {
                case 0:
                    return (null, null, NotFound("projects", projectQuery));
                case 1:
                    project = projects[0];
                    break;
                default:
                    return (null, null, Duplicates("project", projectQuery, projects, _ => _.Name));
            }

            var repositoryQuery = parts[1];
            var projectRepositories = project.Repos
                .Where(_ => _.Name.Contains(repositoryQuery, StringComparison.InvariantCultureIgnoreCase))
                .Take(DuplicatesLimit)
                .ToList();
            return projectRepositories.Count switch
            {
                0 => (null, null, $"{NotFound("repositories", repositoryQuery)} in project `{project.Name}`"),
                1 => (ProjectIdentifier.Id(project.Id), projectRepositories[0].Name, null),
                _ => (null, null, Duplicates("repository", repositoryQuery, projectRepositories, _ => _.Name))
            };
        }

        var repositories = (await projectClient.Repositories.Find.GetAllFindAsync(query, top: DuplicatesLimit)).Data ?? new List<RepositoryDetails>();
        return repositories.Count switch
        {
            0 => (null, null, NotFound("repositories", query)),
            1 => (ProjectIdentifier.Key(repositories[0].ProjectKey), repositories[0].Repository, null),
            _ => (null, null, Duplicates("repository", query, repositories, _ => _.Repository))
        };
    }

    public async Task<(ChannelIdentifier Channel, string Message)> SearchChannelAsync(string clientId, string query)
    {
        var chatClient = await _spaceClientProvider.GetChatClientAsync(clientId);
        var channels = (await chatClient.Channels.ListAllChannelsAsync(query, top: DuplicatesLimit)).Data ?? new List<AllChannelsListEntry>();
        return channels.Count switch
        {
            0 => (null, NotFound("channels", query)),
            1 => (ChannelIdentifier.Id(channels[0].ChannelId), null),
            _ => (null, Duplicates("channel", query, channels, _ => _.Name))
        };
    }

    private static string NotFound(string type, string query)
        => $"No {type} found for `{query}`";

    private static string Duplicates<T>(string type, string query, IReadOnlyCollection<T> items, Func<T, string> toString)
        => $"Found more than one {type} matching `{query}`, including {string.Join(", ", items.Select(_ => $"`{toString(_)}`"))}";
}