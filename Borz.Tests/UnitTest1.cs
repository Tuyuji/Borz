namespace Borz.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Console.WriteLine($"Current machine info: {MachineInfo.GetCurrentMachineInfo()}");
        Borz.Init();
    }

    [TearDown]
    public void Teardown()
    {
        Borz.Shutdown();
    }

    [Test]
    public void Atleast_One_Core_Available()
    {
        var threadCount = Borz.GetUsableThreadCount();
        Assert.GreaterOrEqual(threadCount, 1);
        MugiLog.Info($"Usable threads: {threadCount}");
    }
}