using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SummIt.DB;

[Table("Installations")]
public record AppInstallation(
    string ServerUrl,
    [property: Key] string ClientId,
    string ClientSecret
)
{
    public override string ToString()
    {
        return $"ClientId={ClientId};ServerUrl={ServerUrl}";
    }
}