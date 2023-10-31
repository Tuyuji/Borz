namespace Borz.Core.Languages.D;

[BorzUserData]
public record DubPkg(
    string[] Versions,
    string[] Libs,
    string[] Includes
);