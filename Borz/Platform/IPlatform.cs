using System.Runtime.InteropServices;
using ByteSizeLib;

namespace Borz.Platform;

public interface IPlatform
{
    public static IPlatform Instance { get; } = GetPlatform();
    
    public ByteSize GetTotalMemory();
    public ByteSize GetFreeMemory();
    public ByteSize GetAvailableMemory();

    //This isn't for the borz folder, but for the user's config folder
    public string GetUserConfigPath();
    
    private static IPlatform GetPlatform()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxPlatform();
        
        throw new Exception("Platform not supported");
    }
}