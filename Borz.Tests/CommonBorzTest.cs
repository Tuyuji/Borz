namespace Borz.Tests;

public class CommonBorzTest
{
    [OneTimeSetUp]
    public void Setup()
    {
        Console.WriteLine($"Current machine info: {MachineInfo.GetCurrentMachineInfo()}");
        Borz.Init();
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        Borz.Shutdown();
    }
}