using Borz.Helpers;

namespace Borz.Languages.C;

public abstract class CCompiler : Compiler
{
    public bool GenerateSourceDependencies = false;
    public CompileCommands.CompileDatabase? CompileDatabase = null;
    public string ObjectFileExtension = ".o";
    
    protected CCompiler(Options opt) : base(opt)
    {
    }
    
    public abstract ProcUtil.RunOutput CompileObject(Project project, string sourceFile, string outputFile);
    public abstract ProcUtil.RunOutput LinkProject(Project project, string[] objects);
    
    //Returns the dependencies of a object file
    public abstract (bool success, string[] dependencies) GetDependencies(Project project, string objectFile);
    
    public abstract string GetCompiledPchLocation(CProject project);
    public abstract ProcUtil.RunOutput CompilePch(CProject project);
}