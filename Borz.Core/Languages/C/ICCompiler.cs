namespace Borz.Core.Languages.C;

/*
 * Common interface for C/C++ compilers
 */
public interface ICCompiler : ICompiler
{
    UnixUtil.RunOutput CompileObject(Project project, string sourceFile, string outputFile);
    UnixUtil.RunOutput LinkProject(Project unknownProject, string[] objects);

    bool IsSupported(out string reason);
}