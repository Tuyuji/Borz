using System.ComponentModel;
using Borz.Cli.Resources;

namespace Borz.Cli;

internal sealed class LocalDescAttribute : DescriptionAttribute
{
    public string Key { get; }

    public LocalDescAttribute(string key) :
        base(Localization.ResourceManager.GetString(key) ??
             throw new ArgumentException($"Key '{key}' not found in resource file.", nameof(key)))
    {
        Key = key;
    }
}