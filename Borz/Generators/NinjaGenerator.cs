using System.Reflection;
using Borz.Languages.C;

namespace Borz.Generators;

public class NinjaGenerator : Generator
{

    private void WriteTargetVariables(StreamWriter file, Options opt)
    {
        
        if (opt.GetTarget().CompileInfo.TryGetValue("c", out var info))
        {
            file.Write("targetflags_c = ");
            file.Write(info.Arguments);
        }
        
        
        if (opt.GetTarget().CompileInfo.TryGetValue("cpp", out var cppinfo))
        {
            file.Write("\ntargetflags_cpp = ");
            file.Write(cppinfo.Arguments);
        }
        
        
        if (opt.GetTarget().CompileInfo.TryGetValue("c", out info))
        {
            file.Write("\ntargetlinkflags_c = ");
            file.Write(info.LinkArguments);
        }
        
        
        if (opt.GetTarget().CompileInfo.TryGetValue("cpp", out cppinfo))
        {
            file.Write("\ntargetlinkflags_cpp = ");
            file.Write(cppinfo.LinkArguments);
        }
        file.Write("\n");
    }
    
    public override (bool success, string error) Generate(Workspace ws, Options opt)
    {
        var sortedProjects = ws.GetSortedProjectList();
        if (sortedProjects == null)
        {
            //GetSortedProjectList will already log about why it returned null, remind them.
            return (false, "Please see above.");
        }
        
        string warningText = "# Generated by Borz: DO NOT EDIT!\n" +
                             "# Borz version: " + Assembly.GetExecutingAssembly().GetName().Version + "\n" +
                             "# Options config: " + opt.Config + "\n";

        var ninjaFileLoc = Path.Combine(ws.Location, "build.ninja");
        var ninjaFileDir = Path.GetDirectoryName(ninjaFileLoc);
        if (ninjaFileDir == null)
        {
            return (false, "Failed to get directory path for build.ninja");
        }

        string GetPathRel(Project prj, string prjPath)
        {
            if (Path.IsPathFullyQualified(prjPath))
            {
                return prjPath;
            }

            return Path.GetRelativePath(ninjaFileDir, prj.GetPathAbs(prjPath));
        }
        
        var file = new StreamWriter(ninjaFileLoc,
            new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write });
        file.WriteLine(warningText);
        
        WriteTargetVariables(file, opt);
        
        file.WriteLine("rule cc\n command = gcc -MMD $cflags $prjflags $targetflags_c -c $in -o $out\n");
        file.WriteLine("rule cxx\n command = g++ -MMD $cflags $prjflags $targetflags_cpp -c $in -o $out\n");
        file.WriteLine("rule link_cc\n command = gcc $cflags $targetlinkflags_c $in $prjflags -o $out\n");
        file.WriteLine("rule link_cxx\n command = g++ $cflags $targetlinkflags_cpp $in $prjflags -o $out\n");
        
        sortedProjects.ForEach((project) =>
        {
            if (project is not CProject cprj || project is not CppProject cppPrj)
            {
                return;
            }
            
            file.WriteLine($"#Begin project: {project.Name}");

            //Handle per project
            var projectFlagsRule = $"{cprj.Name}_cflags";
            
            //lets fill out some variables we will be reusing per obj
            file.Write($"{projectFlagsRule} = ");
            file.Write(cprj.Symbols ? "-g " : "-s ");
            
            if(cprj.StdVersion == "none")
                file.Write("-nostdlib ");
            else if (cprj.StdVersion != string.Empty)
                file.Write($"-std={cprj.StdVersion} ");
            
            if(cprj.Optimisation != String.Empty)
                file.Write($"-O{cprj.Optimisation} ");
            
            foreach (var define in cprj.GetDefines())
                file.Write(define.Value == null ? $"-D{define.Key} " : $"-D{define.Key}={define.Value} ");
            
            foreach (var include in cprj.GetIncludePaths()) file.Write($"-I{GetPathRel(project, include)} ");
            
            if(cprj.UsePIC) file.Write("-fPIC ");
            
            file.Write("\n");
            
            List<string> objs = new List<string>();

            foreach (var sourceFile in cprj.SourceFiles)
            {
                var rule = sourceFile.EndsWith(".cpp") ? "cxx" : "cc";
                
                var objFileName = Path.GetFileNameWithoutExtension(sourceFile) + ".o";
                var objFileRelBF = Path.GetRelativePath(ninjaFileDir, Path.Combine(cprj.GetIntermediateDirectory(opt), objFileName));
                
                var srcRelBF = GetPathRel(project, sourceFile);
                
                file.WriteLine($"build {objFileRelBF}: {rule} {srcRelBF}\n prjflags = ${projectFlagsRule}");
                objs.Add(objFileRelBF);
            }

            var ouputFile = Path.GetRelativePath(ninjaFileDir, cprj.GetOutputFilePath(opt));
            var linkrule = project.Language == Lang.Cpp ? "link_cxx" : "link_cc";
            
            file.Write($"build {ouputFile}: {linkrule}");
            foreach (var obj in objs)
            {
                file.Write($" {obj}");
            }
            file.Write("\n prjflags =");
            foreach (var libraryPath in cprj.GetLibraryPaths(opt)) file.Write($" -L{GetPathRel(project, libraryPath)}");
            foreach (var library in cprj.GetLibraries(opt)) file.Write($" -l{library}");
            if (cprj.StaticStdLib)
            {
                file.Write(" -static-libgcc");
                file.Write(" -static-libstdc++");
                file.Write(" -Wl,-Bstatic");
                file.Write(" -lstdc++");
                file.Write(" -lpthread");
                file.Write(" -Wl,-Bdynamic");
            }
            switch (project.Type)
            {
                case BinType.SharedObj:
                    file.Write(" -shared");
                    break;
                case BinType.StaticLib:
                    file.Write(" -static");
                    break;
            }
        });
        
        file.Write("\n");
        file.Close();
        return (false, "TODO");
    }
}