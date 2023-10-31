namespace Borz.Core;

public interface ICompiler
{
    public bool JustLog { get; set; }

    string GetFriendlyName();
}