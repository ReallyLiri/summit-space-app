using System.Collections.Concurrent;
using System.Web;
using CliWrap;
using JetBrains.Space.Client;
using Microsoft.Extensions.Caching.Memory;
using SummIt.Extensions;
using SummIt.Services.Space;

namespace SummIt.Services.Summarize;

public class RepositorySummarizingService : IRepositorySummarizingService
{
    private readonly ILogger<RepositorySummarizingService> _logger;
    private readonly ISpaceClientProvider _spaceClientProvider;
    private readonly ITextService _textService;

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = 1024
    });

    private static readonly ISet<string> TextExtensions = new HashSet<string>
    {
        "md", "txt", "rtf"
    };

    public RepositorySummarizingService(ILogger<RepositorySummarizingService> logger, ISpaceClientProvider spaceClientProvider, ITextService textService)
    {
        _logger = logger;
        _spaceClientProvider = spaceClientProvider;
        _textService = textService;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetRepositoryKeywordsAsync(
        string clientId,
        ProjectIdentifier projectIdentifier,
        string repository
    )
    {
        var projectClient = await _spaceClientProvider.GetProjectClientAsync(clientId);
        var cloneUrl = await GetCloneUrlAsync(clientId, projectIdentifier, repository, projectClient);
        if (string.IsNullOrEmpty(cloneUrl))
        {
            return null;
        }

        return await WithTempDirectoryAsync(async tempDirectoryPath =>
        {
            await CloneRepositoryAsync(cloneUrl, tempDirectoryPath);
            return await ExtractKeywordsFromFileSystemAsync(tempDirectoryPath);
        });
    }

    private async Task<string> GetCloneUrlAsync(
        string clientId,
        ProjectIdentifier projectIdentifier,
        string repository,
        ProjectClient projectClient
    )
    {
        var repositoryUrls = await projectClient.Repositories.UrlAsync(projectIdentifier, repository);
        var gitUrl = repositoryUrls.HttpUrl;
        if (string.IsNullOrEmpty(gitUrl))
        {
            _logger.LogError($"Missing repository '{projectIdentifier}/{repository}' url");
            return null;
        }

        var applicationClient = await _spaceClientProvider.GetApplicationClientAsync(clientId);
        var application = await _cache.GetOrAddAsync(
            (clientId, "application"),
            async () => await applicationClient.GetApplicationAsync(ApplicationIdentifier.Me),
            TimeSpan.FromHours(1)
        );
        var token = await _cache.GetOrAddAsync(
            (clientId, "token"),
            async () => await applicationClient.BearerTokenAsync(ApplicationIdentifier.Me),
            TimeSpan.FromMinutes(1)
        );
        
        return gitUrl.Replace(
            "https://",
            $"https://{HttpUtility.UrlEncode(application.Name)}:{HttpUtility.UrlEncode(token)}@"
        );
    }

    private static async Task CloneRepositoryAsync(string cloneUrl, string directoryPath)
    {
        await Cli.Wrap("git")
            .WithArguments($"clone --depth 1 {cloneUrl}")
            .WithWorkingDirectory(directoryPath)
            .ExecuteAsync();
    }

    private async Task<IReadOnlyDictionary<string, int>> ExtractKeywordsFromFileSystemAsync(string directoryPath)
    {
        var histogram = new ConcurrentDictionary<string, int>();
        foreach (var item in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos())
        {
            await ExtractKeywordsFromItemAsync(item, histogram);
        }

        return histogram;
    }

    private async Task ExtractKeywordsFromItemAsync(FileSystemInfo item, ConcurrentDictionary<string, int> histogram)
    {
        foreach (var token in _textService.TokenizeName(item.Name))
        {
            histogram.Increase(token);
        }

        if (
            !item.Attributes.HasFlag(FileAttributes.Directory) &&
            item.Attributes.HasFlag(FileAttributes.Normal) &&
            TextExtensions.Contains(item.Extension.TrimStart('.').ToLowerInvariant())
        )
        {
            var content = await File.ReadAllTextAsync(item.FullName);
            foreach (var token in _textService.TokenizeText(content))
            {
                histogram.Increase(token);
            }
        }
    }

    private static async Task<T> WithTempDirectoryAsync<T>(Func<string, Task<T>> worker)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            return await worker(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}