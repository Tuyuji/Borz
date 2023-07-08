using AkoSharp;

namespace Borz.Core.Compilers;

[ShortType("Gcc")]
public class GccCompiler : CcCompiler
{
    public override string CCompilerElf => "gcc";
    public override string CppCompilerElf => "g++";

    public override bool IsSupportedExt(out string reason)
    {
        //Make sure gcc is installed
        var res = Utils.RunCmd("gcc", "--version");
        if (res.Exitcode != 0)
        {
            reason = "GCC is not installed.";
            return false;
        }

        /*
         * Example output from gcc --version:
         * gcc (GCC) 12.2.1 20221121 (Red Hat 12.2.1-4)
         * ...
         */
        //Get the first line and split it by spaces
        var split = res.Ouput.Split('\n')[0].Split(' ');
        //Get the version number
        var version = split[2];
        //Check if the version is 10 or higher
        var versionParts = version.Split('.');
        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(versionParts[1]);
        var patch = int.Parse(versionParts[2]);
        //Require at least 12.1 since mold says so.
        if (major >= 12 && minor >= 1)
        {
            reason = "";
            supported = true;
            return true;
        }

        reason = "GCC version is too old. Please install GCC 10 or higher.";
        return false;
    }

    public override string GetFriendlyName()
    {
        return "Gcc";
    }
}