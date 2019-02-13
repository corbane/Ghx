#load "./libs.csx"

using Ghx.RoslynScript;
using Rhino.Geometry;

[Input ("B", "Brep")]
Brep brep;

[Output ("R", "result")]
var result = brep;
