using Borz.Core.Helpers;

namespace Borz.Core.Languages.C;

/*
 * Common interface for C/C++ compilers
 */
public interface ICCompiler : ICompiler
{
    public bool GenerateSourceDependencies { get; set; }
    public CompileCommands.CompileDatabase? CompileDatabase { get; set; }
    public bool OnlyOutputCompileCommands { get; set; }

    UnixUtil.RunOutput CompileObject(Project project, string sourceFile, string outputFile);
    UnixUtil.RunOutput LinkProject(Project project, string[] objects);

    /// <summary>
    /// This will return the dependencies of a source file.
    /// </summary>
    /// <param name="project"></param>
    /// <param name="objectFile"></param>
    /// <param name="dependencies"></param>
    /// <returns>Is successful</returns>
    bool GetDependencies(Project project, string objectFile, out string[] dependencies);

    bool IsSupported(out string reason);

    string GetFriendlyName(bool asLinker = false);
    string GetCompiledPchLocation(CProject project);
    UnixUtil.RunOutput CompilePch(CProject project);
}