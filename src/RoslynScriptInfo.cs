using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Ghx.RoslynScript
{
    public class RoslynScriptInfo : GH_AssemblyInfo
    {
        public override string Name => "RoslynScript";

        public override Bitmap Icon => Ghx.RoslynScript.Properties.Resources.ico_ghx_cscript;

        public override string Description => "Run and debug a CSharp script inside Grasshopper component with Visual Studio Code";

        public override Guid Id => new Guid("99076411-ed2a-43b8-aa88-944abd81f137");
        
        public override string AuthorName => "Jean-marie Vrecq";

        public override string AuthorContact => "";
    }
}
