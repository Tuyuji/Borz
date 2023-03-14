using MoonSharp.Interpreter;

namespace Borz;

[BorzUserData]
public enum BinType: uint
{
    ConsoleApp = 0,
    SharedObj = 1,
    StaticLib = 2,
    WindowsApp = 3
}