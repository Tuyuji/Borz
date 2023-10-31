using AkoSharp;
using Borz.Core.Helpers;
using Borz.Core.Languages.C;
using Microsoft.VisualBasic;

namespace Borz.Core.Languages.D;

[ShortType("Gdc")]
public class GDCCompiler : ICCompiler
{
    public bool JustLog { get; set; }

    public string GetFriendlyName()
    {
        return "GDC";
    }

    public bool GenerateSourceDependencies { get; set; }
    public CompileCommands.CompileDatabase? CompileDatabase { get; set; }
    public bool OnlyOutputCompileCommands { get; set; }

    public UnixUtil.RunOutput CompileObject(Project inProj, string sourceFile, string outputFile)
    {
        if (inProj is not DProject project)
            throw new Exception("Project is not a DProject");

        List<string> cmdArgs = new();

        AddSymbols(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);

        if (GenerateSourceDependencies)
            cmdArgs.Add("-MMD");

        AddVersion(project, ref cmdArgs);
        AddIncludes(project, ref cmdArgs);
        AddPic(project, ref cmdArgs);

        cmdArgs.Add("-o");
        cmdArgs.Add(outputFile);

        cmdArgs.Add("-c");
        cmdArgs.Add(sourceFile);

        CompileDatabase?.Add(new CompileCommands.CompileCommand
        {
            Directory = project.ProjectDirectory,
            Arguments = cmdArgs.ToArray(),
            Command = "gdc" + " " + Strings.Join(cmdArgs.ToArray(), " "),
            File = sourceFile,
            Output = outputFile
        });

        return Utils.RunCmd("gdc",
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
    }

    private void AddLibraries(DProject project, ref List<string> cmdArgs)
    {
        foreach (var library in project.GetLibraries()) cmdArgs.Add($"-l{library}");
    }

    public void AddLibraryPaths(CProject project, ref List<string> args)
    {
        foreach (var libraryPath in project.GetLibraryPaths()) args.Add($"-L{libraryPath}");
    }

    private void AddPic(DProject project, ref List<string> cmdArgs)
    {
        if (project.UsePIC) cmdArgs.Add("-fPIC");
    }

    private void AddVersion(DProject project, ref List<string> cmdArgs)
    {
        foreach (var version in project.Versions)
        {
            cmdArgs.Add("-fversion=" + version);
        }
    }

    public UnixUtil.RunOutput LinkProject(Project inProj, string[] objects)
    {
        if (inProj is not DProject project)
            throw new Exception("Project is not a DProject");

        //This includes the output file
        var outputPath = project.GetOutputFilePath();

        if (project.Type == BinType.StaticLib)
        {
            return Utils.RunCmd("ar", $"-rcs \"{outputPath}\" " + string.Join(" ", objects.ToArray()),
                project.ProjectDirectory, JustLog);
        }

        List<string> cmdArgs = new();

        cmdArgs.Add("-o");
        cmdArgs.Add(outputPath);

        AddVersion(project, ref cmdArgs);
        AddPhobos(project, ref cmdArgs);
        AddStdVersion(project, ref cmdArgs);

        cmdArgs.AddRange(objects);

        foreach (var rpath in project.GetRPaths(project.GetPathAbs(project.OutputDirectory)))
            cmdArgs.Add($"-Wl,-rpath=$ORIGIN/{rpath}");

        AddLibraryPaths(project, ref cmdArgs);
        AddLibraries(project, ref cmdArgs);
        AddSymbols(project, ref cmdArgs);

        switch (project.Type)
        {
            case BinType.SharedObj:
                cmdArgs.Add("-shared");
                break;
            case BinType.StaticLib:
                cmdArgs.Add("-static");
                break;
        }

        CompileDatabase?.Add(new CompileCommands.CompileCommand
        {
            Directory = project.ProjectDirectory,
            Arguments = cmdArgs.ToArray(),
            Command = "gdc" + " " + Strings.Join(cmdArgs.ToArray(), " "),
            File = string.Empty,
            Output = outputPath
        });

        return Utils.RunCmd("gdc",
            Strings.Join(cmdArgs.ToArray())!, project.ProjectDirectory, JustLog);
    }

    public bool GetDependencies(Project project, string objectFile, out string[] dependencies)
    {
        var depFile = Path.Combine(project.IntermediateDirectory,
            Path.GetFileNameWithoutExtension(objectFile) + ".deps");

        dependencies = Array.Empty<string>();

        if (!File.Exists(depFile))
            return false;

        var dep = PosixDepParser.Parse(File.ReadAllText(depFile));
        var objFileAbs = Path.Combine(project.IntermediateDirectory, objectFile);
        if (!dep.ContainsKey(objFileAbs)) return false;
        dependencies = dep[objFileAbs].ToArray();
        return true;
    }

    public void AddIncludes(DProject project, ref List<string> args)
    {
        foreach (var include in project.GetIncludePaths()) args.Add($"-I{include}");
    }

    public bool IsSupported(out string reason)
    {
        reason = "Not implemented";
        return true;
    }

    public string GetFriendlyName(bool asLinker = false)
    {
        return "GDC";
    }

    public string GetCompiledPchLocation(CProject project)
    {
        throw new NotImplementedException();
    }

    public UnixUtil.RunOutput CompilePch(CProject project)
    {
        throw new NotImplementedException();
    }

    public void AddSymbols(DProject project, ref List<string> args)
    {
        args.Add(project.Symbols ? "-fdebug" : "-frelease");
        args.Add(project.Symbols ? "-g" : "-s");
    }

    public void AddStdVersion(CProject project, ref List<string> args)
    {
        if (project.StdVersion == "none")
        {
            args.Add("-nostdlib");
            return;
        }

        if (project.StdVersion != string.Empty)
        {
            args.Add("-std=" + project.StdVersion);
        }
    }

    public void AddPhobos(DProject project, ref List<string> args)
    {
        if (project.PhobosType == PhobosType.NotSet)
            return;

        switch (project.PhobosType)
        {
            case PhobosType.None:
                args.Add("-nophoboslib");
                return;
            case PhobosType.Static:
                args.Add("-static-libphobos");
                return;
            case PhobosType.Shared:
                args.Add("-shared-libphobos");
                return;
            default:
                return;
        }
    }
}