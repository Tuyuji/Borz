namespace Borz.PkgConfig;

public record PkgConfigInfo(
    string Name,
    string Version,
    string[] Libs,
    string[] CFlags
);