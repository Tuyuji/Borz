namespace Borz.Core.Generators;

[AttributeUsage(AttributeTargets.Class)]
public class FriendlyNameAttribute : Attribute
{
    public string FriendlyName { get; }

    public FriendlyNameAttribute(string friendlyName)
    {
        FriendlyName = friendlyName;
    }
}