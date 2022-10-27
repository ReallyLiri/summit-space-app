using System.Text.Json;

namespace SummIt.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj) => JsonSerializer.Serialize(obj);
    public static T FromJson<T>(this string str) => JsonSerializer.Deserialize<T>(str);
}