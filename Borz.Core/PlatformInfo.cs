namespace Borz.Core;

[BorzUserData]
public class PlatformInfo
{
    public string ExecutablePrefix;
    public string ExecutableExtension;

    public string SharedLibraryPrefix;
    public string SharedLibraryExtension;

    public string StaticLibraryPrefix;
    public string StaticLibraryExtension;

    public Dictionary<string, string?>? Defines;
    public string CompilerFlags;
    public string LinkerFlags;

    public List<string>? SupportedCompilers;

    public static PlatformInfo New()
    {
        return new PlatformInfo();
    }
}