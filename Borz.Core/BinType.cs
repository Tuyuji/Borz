namespace Borz.Core;

[BorzUserData]
public enum BinType : uint
{
    Unknown = 0,
    ConsoleApp = 1,
    SharedObj = 2,
    StaticLib = 3,
    WindowsApp = 4,
}