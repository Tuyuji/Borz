using System.ComponentModel;
using Borz.Resources;

namespace Borz;

internal sealed class LocalDescAttribute : DescriptionAttribute
{
    public string Key { get; }

    public LocalDescAttribute(string key) :
        base(Lang.ResourceManager.GetString(key) ??
             throw new ArgumentException($"Key '{key}' not found in resource file.", nameof(key)))
    {
        Key = key;
    }
}