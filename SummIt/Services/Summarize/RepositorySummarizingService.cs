using System.Collections.Concurrent;
using System.Text;
using System.Web;
using CliWrap;
using CliWrap.Exceptions;
using JetBrains.Space.Client;
using JetBrains.Space.Common;
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
            async () => await _spaceClientProvider.GetBearerTokenAsync(clientId),
            TimeSpan.FromMinutes(1)
        );

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError($"Failed to acquire bearer token for client-id '{clientId}'");
            return null;
        }

        return gitUrl.Replace(
            "https://",
            $"https://{HttpUtility.UrlEncode(application.Name)}:{HttpUtility.UrlEncode(token)}@"
        );
    }

    private async Task CloneRepositoryAsync(string cloneUrl, string directoryPath)
    {
        _logger.LogInformation($"Cloning repository '{cloneUrl}'");
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();
        try
        {
            await Cli.Wrap("git")
                .WithArguments($"clone --depth 1 {cloneUrl}")
                .WithWorkingDirectory(directoryPath)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();
        }
        catch (CommandExecutionException commandExecutionException)
        {
            throw new ApplicationException($"Process failed.\nStdout:\n{stdOutBuffer}\nStderr:\n{stdErrBuffer}", commandExecutionException);
        }
    }

    private async Task<IReadOnlyDictionary<string, int>> ExtractKeywordsFromFileSystemAsync(string directoryPath)
    {
        var histogram = new ConcurrentDictionary<string, int>();
        foreach (var item in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
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