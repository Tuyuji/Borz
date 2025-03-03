using System.Diagnostics;
using Borz.Languages.C;

namespace Borz.Tests;

[TestFixture]
public class BuildTest : CommonBorzTest
{
    [Test]
    public void Atleast_One_Core_Available()
    {
        var threadCount = Borz.GetUsableThreadCount();
        Assert.GreaterOrEqual(threadCount, 1);
        MugiLog.Info($"Usable threads: {threadCount}");
    }
    
    [Test]
    public void BuildHelloWorld()
    {
        //Do a simple hello world compile
        //make a temp directory

        //Simple hello world in C
        const string MainFile = @"
#include <stdio.h>
int main(int argc, char** argv) {
    printf(""Hello, World!"");
    return 0;
}
";
        
        var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(testDir);

        var mainFilePath = Path.Combine(testDir, "main.c");
        File.WriteAllText(mainFilePath, MainFile);
        
        var ws = new Workspace(testDir);

        var proj = (CProject)Project.Create(Lang.C, "unit_testing", BinType.ConsoleApp, testDir);
        ws.Add(proj);
        
        proj.OutputDirectory = testDir;
        proj.AddSourceFile(mainFilePath);
        
        
        ws.Compile(new Options());
        
        string ExpectedExecutablePath = Path.Combine(testDir, $"{MachineInfo.HostMachine.ExePrefix}unit_testing{MachineInfo.HostMachine.ExeExt}");
        Assert.True(File.Exists(ExpectedExecutablePath));
        
        //Execute the program
        var proc = new Process();
        proc.StartInfo.FileName = ExpectedExecutablePath;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.UseShellExecute = false;
        proc.Start();
        proc.WaitForExit();
        var output = proc.StandardOutput.ReadToEnd();
        Assert.That(proc.ExitCode, Is.EqualTo(0));
        Assert.That(output, Is.EqualTo("Hello, World!"));
    }
    
}