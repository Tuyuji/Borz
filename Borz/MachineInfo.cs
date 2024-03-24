using System.Runtime.InteropServices;
using AkoSharp;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace Borz;

public enum Endianness
{
    Unknown,
    Big,
    Little
}

[MoonSharpUserData]
public class LangCompileInfo
{
    public Dictionary<string, string?> Defines = new();
    public List<string> Arguments = new();
    public List<string> LinkArguments = new();
}

//borz c --target linux-risv64-sifive-desktop-gcc

/*
 * Class is used to identify the machine compiling and what the target machine its targeting.
 */
[MoonSharpUserData]
public class MachineInfo
{
    //linux, macos, windows, android
    public string OS;
    //x86_64, x86, arm64, arm32, riskv64
    public string Arch;
    
    //Example: intel, amd, snapdragon, sifive
    public string Vendor { get; init; } = "unknown";
    //Example: desktop, mobile, server.
    public string Environment { get; init; } = "unknown";
    //Example: gcc, msvc or clang
    public string ABI { get; init; } = "unknown";

    public Endianness Endian;

    public Dictionary<string, string> Binaries = new();
    //What compilers it should use for a language
    //c -> gcc
    public Dictionary<string, string> Compilers = new();

    //Contains extra info for langs.
    public Dictionary<string, LangCompileInfo> CompileInfo = new();
    
    public string ExePrefix = "";
    public string ExeExt = "";
    public string SharedLibPrefix = "lib";
    public string SharedLibExt = ".so";
    public string StaticLibPrefix = "lib";
    public string StaticLibExt = ".a";
    
    public static MachineInfo UnknownMachine = new MachineInfo("unknown", "unknown");

    public MachineInfo(string os, string arch)
    {
        this.OS = os;
        this.Arch = arch;
        Endian = GetArchitectureEndianness(Arch);

        switch (os)
        {
            case "windows":
            {
                ExeExt = ".exe";
                SharedLibPrefix = "";
                SharedLibExt = ".dll";
                StaticLibPrefix = "";
                StaticLibExt = ".lib";
            } break;
            case "macos":
            {
                SharedLibExt = ".dylib";
            } break;
        }
    }
    
    [MoonSharpVisible(true)]
    private static MachineInfo @new(string os, string arch) => new MachineInfo(os, arch);
    
    public string GetBinaryPath(string name, string @default)
    {
        return !Binaries.ContainsKey(name) ? @default : Binaries[name];
    }
    
    public bool IsVendorValid() => Vendor != "unknown";

    public bool IsEnvValid() => Environment != "unknown";

    public bool IsAbiValid() => ABI != "unknown";

    public override string ToString()
    {
        string tuple = $"{OS}-{Arch}";
        if (IsVendorValid())
            tuple += $"-{Vendor}";
        if (IsEnvValid())
            tuple += $"-{Environment}";
        if (IsAbiValid())
            tuple += $"-{ABI}";
        return tuple;
    }

    public static MachineInfo? Parse(string input, MachineInfo? optBase = null)
    {
        if (!input.Contains('-'))
        {
            return optBase != null ? new MachineInfo(input, optBase.Arch) : null;
        }
        
        Queue<string> stack = new Queue<string>(input.Split('-'));
        if (stack.Count < 2)
        {
            return null;
        }
        
        var os = stack.Dequeue();
        var arch = stack.Dequeue();
        var vendor = stack.Count != 0 ? stack.Dequeue() : "unknown";
        var env = stack.Count != 0 ? stack.Dequeue() : "unknown";
        var abi = stack.Count != 0 ? stack.Dequeue() : "unknown";

        return new MachineInfo(os, arch)
        {
            Vendor = vendor,
            Environment = env,
            ABI = abi
        };
    }

    //Borz sticks to a standard for its Machine info, keep to it.
    public static string ArchitectureToString(Architecture arch)
    {
        switch (arch)
        {
            case Architecture.X86:
                return "x86";
            case Architecture.X64:
                return "x86_64";
            case Architecture.Arm:
                return "arm32";
            case Architecture.Arm64:
                return "arm64";
            case Architecture.Wasm:
                return "wa32";
            case Architecture.Ppc64le:
                return "ppc64";
            default:
                return "unknown";
        }
    }

    public static Endianness GetArchitectureEndianness(string arch)
    {
        switch (arch)
        {
            case "x86_64":
            case "x86":
            case "wa32":
            case "arm64":
            case "arm32":
                return Endianness.Little;
            case "ppc64":
                return Endianness.Big;
            default:
                return Endianness.Unknown;
        }
    }
    
    public static string GetCurrentMachineOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macos";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return "freebsd";
        }

        return "unknown";
    }
    
    public static MachineInfo GetCurrentMachineInfo()
    {
        var os = GetCurrentMachineOS();
        var arch = ArchitectureToString(RuntimeInformation.ProcessArchitecture);
        
        return new MachineInfo(os, arch);
    }
}