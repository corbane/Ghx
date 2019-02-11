using Grasshopper.Kernel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ghx.RoslynScript
{
    public static class LanguageService
    {
        public delegate void OnRecompiledHandler(CompilationResult result);

        public static Exception Exeception { get; private set; }


        public struct CompilationResult
        {
            public string Error { get; internal set; }
            public string AssemblyLocation { get; internal set; }
            public bool IsNewAssembly { get; internal set; }
        }
        

        public static CompilationResult Create(string sourcePath, bool watch = false, OnRecompiledHandler callback = null)
        {
            var result = new CompilationResult()
            {
                Error = null,
                AssemblyLocation = null,
                IsNewAssembly = false
            };

            try
            {
                if (!File.Exists(sourcePath))
                {
                    result.Error = $"File ${sourcePath} not found";
                    return result;
                }

                if (watch)
                    Watch(sourcePath, callback);

                var directory = Path.GetDirectoryName(sourcePath);
                var assemblyName = Path.GetFileNameWithoutExtension(sourcePath);
                var dllPath = Path.Combine(directory, assemblyName + ".dll");

                var finfo = new FileInfo(dllPath);
                if (finfo.Exists && finfo.Length > 0 &&
                    File.GetLastWriteTime(sourcePath) <= finfo.LastWriteTime)
                {
                    result.AssemblyLocation = dllPath;
                    return result;
                }

            // ParseSyntaxTree

                var code = File.ReadAllText(sourcePath);

                var ParseOptions = new CSharpParseOptions()
                               .WithKind(SourceCodeKind.Script)
                               .WithDocumentationMode(DocumentationMode.Parse)
                               .WithPreprocessorSymbols(new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__", "TRACE", "DEBUG" });

                var tree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions, sourcePath, Encoding.UTF8);
                
                if (tree.GetDiagnostics().Count() != 0)
                {
                    result.Error = String.Join("\n", from d in tree.GetDiagnostics()
                                                          select $"{d.GetMessage()} ({d.Location.GetLineSpan ()})");
                    return result;
                }

            // CreateScriptCompilation

                if (!Environment.Is64BitProcess)
                {
                    result.Error = "Not x64 platform";
                    return result;
                }

                var compilationOptions = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    scriptClassName: "Program",
                    usings: null,
                    optimizationLevel: OptimizationLevel.Debug,
                    platform: Platform.X64,
                    sourceReferenceResolver: SourceFileResolver.Default,
                    metadataReferenceResolver: ScriptMetadataResolver.Default,
                    assemblyIdentityComparer: AssemblyIdentityComparer.Default,
                    allowUnsafe: false
                );

                var compilation = CSharpCompilation.CreateScriptCompilation(
                    assemblyName,
                    options: compilationOptions,
                    references: GetInitialReferences()
                );
                
                if (compilation.GetDiagnostics().Count() != 0)
                {
                    result.Error = String.Join("\n", from d in tree.GetDiagnostics()
                                                           select $"{d.GetMessage()} ({d.Location.GetLineSpan()})");
                    return result;
                }

                compilation = compilation.AddSyntaxTrees(tree);

                IEnumerable<MetadataReference> GetInitialReferences()
                {
                    var assemblies = new[]
                    {
                        typeof(Input).Assembly,
                        typeof(RhinoApp).Assembly,
                        typeof(GH_Document).Assembly
                    };

                    var refs = from a in assemblies
                               select MetadataReference.CreateFromFile(a.Location);

                    var stdPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                    return new List<MetadataReference>()
                    {
                        MetadataReference.CreateFromFile (Path.Combine(stdPath, "mscorlib.dll")),
                        MetadataReference.CreateFromFile (Path.Combine(stdPath, "System.dll")),
                        MetadataReference.CreateFromFile (Path.Combine(stdPath, "System.Core.dll")),
                        MetadataReference.CreateFromFile (Path.Combine(stdPath, "System.Runtime.dll"))
                    };
                }
                
            // EmitAssembly
                
                var pdbPath = Path.Combine(directory, assemblyName + ".pdb");
                var xmlPath = Path.Combine(directory, assemblyName + ".xml");

                var options = new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb
                );

                using (var modFs = new FileStream(dllPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                using (var pdbFs = new FileStream(pdbPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                {
                    EmitResult r = compilation.Emit(modFs, pdbFs, options: options);

                    if (!r.Success)
                    {
                        result.Error = String.Join("\n", from d in r.Diagnostics
                                                              select $"{d.GetMessage()} ({d.Location.GetLineSpan()})");
                        return result;
                    }

                    result.AssemblyLocation = dllPath;
                    result.IsNewAssembly = true;

                    return result;
                }
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                return result;
            }
        }


        // TODO ResolverService
        public static string ResolveScriptUri (string path)
        {
            if (Path.GetExtension(path) != ".csx")
                path = path + ".csx";

            return path;
        }
        
        
        private static Dictionary<string, SyncronizedSource> syncronizedSources = new Dictionary<string, SyncronizedSource> ();


        private class SyncronizedSource
        {
            public GH_FileWatcher Watcher;
            public OnRecompiledHandler Callback;

            public SyncronizedSource (OnRecompiledHandler callback, GH_FileWatcher watcher)
            {
                Watcher = watcher;
                Callback = callback;
            }
        }
        
        public static void Watch(string sourcePath, OnRecompiledHandler callback)
        {
            if (syncronizedSources.ContainsKey(sourcePath))
            {
                syncronizedSources[sourcePath].Callback = callback;
                return;
            }

            var watcher = GH_FileWatcher.CreateFileWatcher(sourcePath, GH_FileWatcherEvents.All, (p) =>
            {
                var result = Create(sourcePath);
                syncronizedSources[sourcePath].Callback?.Invoke(result);
            });

            syncronizedSources.Add(sourcePath, new SyncronizedSource (callback, watcher));
        }

        public static void Unwatch(string sourcePath)
        {
            if (!syncronizedSources.ContainsKey(sourcePath))
                return;

            var src = syncronizedSources[sourcePath];
            src.Watcher.Dispose();
            syncronizedSources.Remove(sourcePath);
        }
    }
}
