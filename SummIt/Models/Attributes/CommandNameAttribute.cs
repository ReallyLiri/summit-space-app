namespace SummIt.Models.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class CommandNameAttribute : Attribute
{
    public CommandNameAttribute(string name)
    {
        Name = name;
    }
    
    public string Name { get; }
}