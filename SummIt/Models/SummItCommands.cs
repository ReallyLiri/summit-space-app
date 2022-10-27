using System.ComponentModel;
using JetBrains.Annotations;
using SummIt.Models.Attributes;

namespace SummIt.Models;

public enum SummItCommands
{
    [CommandName("help")]
    [Description("Show this help")]
    [UsedImplicitly]
    Help,
    
    [CommandName("repo")]
    [Description("Get quick summary of a repository. Usage: \"/repo <[project/]repostiroy>\"")]
    Repository,
    
    [CommandName("channel")]
    [Description("Get quick summary of a channel. Usage: \"/channel <channel>\"")]
    Channel,
}