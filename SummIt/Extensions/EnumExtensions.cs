using System.Reflection;

namespace SummIt.Extensions;

public static class EnumExtensions
{
    public static string GetText<TAttribute>(this Enum enumValue, Func<TAttribute, string> getText) where TAttribute : Attribute
    {
        var customAttribute = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
        return customAttribute != null ? getText(customAttribute) : string.Empty;
    }
}