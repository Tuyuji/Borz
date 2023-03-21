namespace Borz.Core.PkgConfig;

public record PkgConfigInfo(
    string Name,
    string Version,
    string[] Libs,
    string[] CFlags
);