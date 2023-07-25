using AkoSharp;

namespace Borz.Core.Compilers;

[ShortType("Gcc")]
public class GccCompiler : CcCompiler
{
    private string compilerElf = "gcc";
    private string cppCompilerElf = "g++";

    public override string CCompilerElf => compilerElf;
    public override string CppCompilerElf => cppCompilerElf;

    public GccCompiler()
    {
        if (Borz.BuildConfig.HostPlatform != Borz.BuildConfig.TargetPlatform)
        {
            //Cross compiling!
            if (Borz.BuildConfig.HostPlatform != Lua.Platform.Linux)
            {
                throw new NotSupportedException("Cross compiling is only supported on Linux for now.");
            }

            var crossTargetTable = Borz.Config.Get("linux64", Borz.BuildConfig.TargetPlatform);
            if (crossTargetTable == null)
            {
                throw new NotSupportedException(
                    $"Cross compiling to {Borz.BuildConfig.TargetPlatform} is not supported yet.");
            }

            compilerElf = crossTargetTable["c"];
            cppCompilerElf = crossTargetTable["cxx"];
        }
    }

    public override bool IsSupportedExt(out string reason)
    {
        //Make sure gcc is installed
        var res = Utils.RunCmd(CCompilerElf, "--version");
        if (res.Exitcode != 0)
        {
            reason = "GCC is not installed.";
            return false;
        }

        string[] split;

        string? version;
        string[]? versionParts;
        int major;
        int minor;
        int patch;

        if (compilerElf == "emcc")
        {
            //Emscripten is a special case
            //example line:
            //emcc (Emscripten gcc/clang-like replacement + linker emulating GNU ld) 3.1.42 (6ede0b8fc1c979bb206148804bfb48b472ccc3da)
            split = res.Ouput.Split('\n')[0].Split(' ');
            version = split[9];
            versionParts = version.Split('.');
            major = int.Parse(versionParts[0]);
            minor = int.Parse(versionParts[1]);
            patch = int.Parse(versionParts[2]);
            if (major >= 3 && minor >= 1)
            {
                reason = "";
                supported = true;
                return true;
            }

            reason = "Emscripten version is too old. Please install Emscripten 3.1 or higher.";
            return false;
        }

        /*
         * Example output from gcc --version:
         * gcc (GCC) 12.2.1 20221121 (Red Hat 12.2.1-4)
         * ...
         */
        //Get the first line and split it by spaces
        split = res.Ouput.Split('\n')[0].Split(' ');
        //Get the version number
        version = split[2];
        //Check if the version is 10 or higher
        versionParts = version.Split('.');
        if (!int.TryParse(versionParts[0], out major))
        {
            //Unknown, just let it pass
            supported = true;
            reason = "";
            return true;
        }

        minor = int.Parse(versionParts[1]);
        patch = int.Parse(versionParts[2]);
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