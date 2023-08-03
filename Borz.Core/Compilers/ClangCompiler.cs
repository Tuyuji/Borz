using AkoSharp;

namespace Borz.Core.Compilers;

[ShortType("Clang")]
public class ClangCompiler : CcCompiler
{
    public Version RequiredVersion = new(13, 0, 0);

    public override string CCompilerElf => "clang";
    public override string CppCompilerElf => "clang++";

    public override bool IsSupportedExt(out string reason)
    {
        //Make sure clang is installed
        var res = Utils.RunCmd(CCompilerElf, "--version");
        if (res.Exitcode != 0)
        {
            reason = "Clang is not installed.";
            return false;
        }

        /*
         * Example output from clang --version:
         *  clang version 15.0.7 (Fedora 15.0.7-2.fc37)
            Target: x86_64-redhat-linux-gnu
            Thread model: posix
            InstalledDir: /usr/bin
         */

        //Get the first line and split it by spaces
        var split = res.Ouput.Split('\n')[0].Split(' ');
        //Get the version number
        var version = split[2];
        //make sure clang is version 12 or higher
        var versionParts = version.Split('.');
        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(versionParts[1]);
        var patch = int.Parse(versionParts[2]);
        var currentVersion = new Version(major, minor, patch);
        if (currentVersion >= RequiredVersion)
        {
            reason = "";
            supported = true;
            return true;
        }

        reason = $"Clang version is too old. Please install Clang {RequiredVersion} or higher.";
        return false;
    }

    public override string GetFriendlyName()
    {
        return "Clang";
    }
}