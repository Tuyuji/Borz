using Borz.PkgConfig;

namespace Borz.PkgConfig;

public enum VersionType : uint
{
    None = 0,
    GTOrEq = 1,
    LTOrEq = 2,
    Eq = 3
}

public record PkgConfigInfo(
    string Name,
    string Version,
    string[] Libs,
    string[] CFlags
);

public static class PkgConfig
{
    private static readonly string pkgconifg = "pkg-config";


    private static ProcUtil.RunOutput RunPkgConfig(string cmd)
    {
        return ProcUtil.RunCmd(pkgconifg, cmd);
    }

    private static string GetPkgVersionFormat(VersionType op, string version)
    {
        switch (op)
        {
            case VersionType.GTOrEq:
                return $">= {version}";
            case VersionType.LTOrEq:
                return $"<= {version}";
            case VersionType.Eq:
                return $"= {version}";
            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }
    }

    public static bool DoesPkgExist(string name, VersionType op = VersionType.None, string version = "")
    {
        var cmd = "--exists ";

        if (op != VersionType.None)
            cmd += $"\"{name} {GetPkgVersionFormat(op, version)}\"";
        else
            cmd += $"{name}";

        var result = RunPkgConfig(cmd);
        return result.Exitcode == 0;
    }

    public static PkgConfigInfo? GetPackage(string name, VersionType op = VersionType.None, string version = "")
    {
        if (!DoesPkgExist(name, op, version))
            return null;

        var nameVersion = "";
        nameVersion = op == VersionType.None ? name : $"\"{name} {GetPkgVersionFormat(op, version)}\"";

        var modVersion = "";
        {
            var versionOutput = RunPkgConfig($"--modversion {nameVersion}");
            if (versionOutput.Exitcode != 0)
                return null;
            modVersion = versionOutput.Ouput;
        }

        var libs = "";
        {
            var libsOutput = RunPkgConfig($"--libs {nameVersion}");
            if (libsOutput.Exitcode != 0)
                return null;
            libs = libsOutput.Ouput;
            libs = libs.Trim();
        }

        var cflags = "";
        {
            var cflagsOutput = RunPkgConfig($"--cflags {nameVersion}");
            if (cflagsOutput.Exitcode != 0)
                return null;
            cflags = cflagsOutput.Ouput;
            cflags = cflags.Trim();
        }

        return new PkgConfigInfo(name, modVersion, libs.Split(' '), cflags.Split(' '));
    }
}