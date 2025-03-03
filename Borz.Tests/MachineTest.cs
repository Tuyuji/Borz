namespace Borz.Tests;

public class MachineTest
{
    [Test]
    public void AssureValidInfos()
    {
        var mach = MachineInfo.Parse("linux-x86_64");
        Assert.IsNotNull(mach);
        
        mach = MachineInfo.Parse("macos-x86_64");
        Assert.IsNotNull(mach);
        
        mach = MachineInfo.Parse("windows-x86_64");
        Assert.IsNotNull(mach);
        
        mach = MachineInfo.Parse("linux-arm64");
        Assert.IsNotNull(mach);
        
        mach = MachineInfo.Parse("windows-arm32");
        Assert.IsNotNull(mach);
    }

    [Test]
    public void ExtendedParse()
    {
        var mach = MachineInfo.Parse("linux-x86_64-desktop");
        Assert.IsNotNull(mach);
        Assert.AreEqual("linux", mach.OS);
        Assert.AreEqual("x86_64", mach.Arch);
        Assert.AreEqual("desktop", mach.Vendor);
    }
}