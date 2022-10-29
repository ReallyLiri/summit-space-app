using System.ComponentModel;
using JetBrains.Annotations;
using SummIt.Models.Attributes;

namespace SummIt.Models;

public static class Usages
{
    public const string ChannelUsage = "Usage: \"/repo <project>/<repostiroy>\"";
    public const string RepositoryUsage = "Usage: \"/channel <channel>\"";
}

public enum SummItCommands
{
    [CommandName("help")] [Description("Show this help")] [UsedImplicitly]
    Help,

    [CommandName("repo")] [Description($"Get quick summary of a code repository. {Usages.ChannelUsage}")]
    Repository,

    [CommandName("channel")] [Description($"Get quick summary of a channel. {Usages.RepositoryUsage}")]
    Channel,
}