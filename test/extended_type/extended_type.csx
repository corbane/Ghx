#load "./libs.csx"

using Ghx.RoslynScript;
using Rhino;
using Rhino.Geometry;

class CustomMesh : Mesh
{}

[Input ("ICM")]
Mesh ICMesh;

[Output ("OCM")]
Mesh result = new Mesh ();
result.Append (ICMesh);
result.Scale (2.0);

return;


