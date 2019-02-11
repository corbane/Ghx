using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Ghx.RoslynScript
{
    public static class CodeEditor
    {
        private static Process process;

        public static bool IsOpen => process == null ? false : !process.HasExited;

        public static void ShowEditor (string sourcePath)
        {
            if(sourcePath == null )
            {
                // show save dialog in Rhino document folder or document
                // then create new csx file
                return;
            }

            if( !File.Exists (sourcePath) )
                CreateNewScriptIfNeed(sourcePath);

            var installDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            var args = $"--user-data-dir \"{installDirectory}/editor/data\" "
                        + $"--extensions-dir \"{installDirectory}/editor/extensions\" "
                        + (IsOpen ? " -r " : "")
                        + $"\"{sourceDirectory}\" -g \"{sourcePath}:0\" ";

            var startInfo = new ProcessStartInfo()
            {
                FileName = "code",
                Arguments = args,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            process = Process.Start(startInfo);
        }


        public static void CreateNewScriptIfNeed (string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                throw new NotImplementedException(); //TODO
            
            AppendLibsFileIfNeed(directory);

            if (File.Exists(path))
                return;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            File.Copy(
                Path.Combine(Path.GetDirectoryName(assemblyLocation), "editor/template.csx"),
                path
            );
        }


        public static void AppendLibsFileIfNeed (string directory)
        {
            var libfile = Path.Combine(directory, "libs.csx");
            if (!File.Exists(libfile))
            {
                var meLocation = Assembly.GetExecutingAssembly().Location;
                File.AppendAllLines(libfile, new[] {
                    $"#r \"{typeof (Rhino.RhinoApp).Assembly.Location}\"",
                    $"#r \"{typeof (Grasshopper.Instances).Assembly.Location}\"",
                    $"#r \"{meLocation}\""
                });
            }
        }


        public static bool VSCodeExists () //TODO change to an nodejs script and test the c# extensions + version
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo("code", "-v") {
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return p.ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
