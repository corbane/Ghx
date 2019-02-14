using Grasshopper.Kernel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Rhino;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ghx.RoslynScript
{
    public static class LanguageService
    {
        public delegate void OnRecompiledHandler(CompilationResult result);

        public static Exception Exeception { get; private set; }

        private static Dictionary<string, Assembly> s_assemblies = new Dictionary<string, Assembly>();

        public struct CompilationResult
        {
            public string Error { get; internal set; }
            public string AssemblyLocation { get; internal set; }
            public Assembly Assembly { get; internal set; }
        }
        
        public static long s_target_process_size = Process.GetCurrentProcess().WorkingSet64 * 2;

        public static CompilationResult Create(string sourcePath, bool watch = false, OnRecompiledHandler callback = null)
        {
            var result = new CompilationResult()
            {
                Error = null,
                AssemblyLocation = null,
                Assembly = null
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
                    if (!s_assemblies.ContainsKey(sourcePath))
                    {
                        s_assemblies.Add(sourcePath, Assembly.Load(File.ReadAllBytes(dllPath)));
                        VerifyMemoryUsage();
                    }

                    result.Assembly = s_assemblies[sourcePath];
                    result.AssemblyLocation = dllPath;
                    return result;
                }

                //
                // ParseSyntaxTree
                //

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

                //
                // CreateScriptCompilation
                //

                if (!Environment.Is64BitProcess)
                {
                    result.Error = "Not x64 platform";
                    return result;
                }

                var compilationOptions = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    scriptClassName: Pascalize(assemblyName) + ".Program",
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

                //
                // EmitAssembly
                //

                var pdbPath = Path.Combine(directory, assemblyName + ".pdb");
                var xmlPath = Path.Combine(directory, assemblyName + ".xml");

                var options = new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb
                );
                
                using (var modFs = new FileStream(dllPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                using (var pdbFs = new FileStream(pdbPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                {
                    // Can be changed with MemoryStream
                    // And save the assembly files with the GH_Document in same directory

                    EmitResult r = compilation.Emit(modFs, pdbFs, options: options);

                    if (!r.Success)
                    {
                        result.Error = String.Join("\n", from d in r.Diagnostics
                                                              select $"{d.GetMessage()} ({d.Location.GetLineSpan()})");
                        return result;
                    }
                }

                result.Assembly = Assembly.Load(File.ReadAllBytes(dllPath));

                if (!s_assemblies.ContainsKey(sourcePath))
                    s_assemblies.Add(sourcePath, result.Assembly);
                else
                    s_assemblies[sourcePath] = result.Assembly;

                result.AssemblyLocation = dllPath;

                VerifyMemoryUsage();

                return result;

                void VerifyMemoryUsage ()
                {
                    var size = Process.GetCurrentProcess().WorkingSet64;
                    if( size > s_target_process_size )
                    {
                        //TODO: Alert user to reload Rhino
                    }
                }

                string Pascalize(string str)
                {
                    var r = new StringBuilder(str.Length);
                    var i = 0;
                    var chars = str.ToCharArray();

                    for(; i != chars.Length; ++i)
                    {
                        var c = chars[i];

                        if (!Char.IsLetter(c))
                            continue;

                        if (Char.IsUpper(c))
                            r.Append(c);
                        else
                            r.Append(Char.ToUpper (c));
                        break;
                    }

                    var needupper = false;

                    for (++i; i != chars.Length; ++i)
                    {
                        var c = chars[i];

                        if (!Char.IsLetterOrDigit(c))
                        {
                            needupper = true;
                        }
                        else if (Char.IsUpper(c))
                        {
                            needupper = false;
                            r.Append(c);
                        }
                        else if(needupper)
                        {
                            needupper = false;
                            r.Append(Char.ToUpper(c));
                        }
                        else
                        {
                            r.Append(c);
                        }
                    }

                    return r.ToString();
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
