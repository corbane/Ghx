#load "./libs.csx"

/*
Source note:
    The debugger must be detached to compile the source
    Derived types are not implemented (eg. `class CustomMesh : Mesh {}`)

- CollectVolatileData_FromSources
- PostProcessData
- OnVolatileDataCollected
- PostProcessData
- OnVolatileDataCollected
*/

using System;
using Rhino.Geometry;
using Ghx.RoslynScript;

/*
Inputs note:
    The order of the inputs is not the same as that of the source
*/

[Input ("B", "Boolean")]
bool tBool;

[Input ("I", "Integer")]
int tInteger;

[Input ("D", "Double")]
double tDouble;

[Input ("S", "String")]
string tString;

[Input ("D", "DateTime")]
DateTime tDate;

[Input ("C", "Color")]
System.Drawing.Color tColor;

[Input ("Id", "Guid")]
Guid tGuid;

[Input ("P", "Point3d")]
Point3d tPoint;

[Input ("V", "Vector3d")]
Vector3d tVector;

[Input ("P", "Plane")]
Plane tPlane;

[Input ("I", "Interval")]
Interval tInterval;

//UVInterval

[Input ("R", "Rectangle3d")]
Rectangle3d tRectangle3d;

[Input ("B", "Box")]
Box tBox;

[Input ("T", "Transform")]
Transform tTransform;

[Input ("L", "Line")]
Line tLine;

[Input ("C", "Circle")]
Circle tCircle;

[Input ("A", "Arc")]
Arc tArc;

[Input ("P", "Polyline")]
Polyline tPolyline;

[Input ("C", "Curve")]
Curve tCurve;

[Input ("S", "Surface")]
Surface tSurface;

[Input ("B", "Brep")]
Brep tBrep;

[Input ("M", "Mesh")]
Mesh tMesh;

[Input ("G", "GeometryBase")]
GeometryBase GeometryBase;

class CustomMesh : Mesh
{
    
}

//[Input ("CM", "Custom Mesh")]
//CustomMesh tCustomMesh;

//[Output ("OCustomMesh", "Custom Mesh output")]
//var OCustomMesh = new CustomMesh ();

//
// Test outputs
//

/*
Outputs note:
    If the name of the field (eg. "OBollean") change the grah links are lost
    If an output is deleted in the source code the connected components input is not updated
    The order of the outputs is not the same as that of the source
*/

[Output ("OBool", "Boolean output")]
var OBollean = true;

[Output ("OInt", "Result")]
var OInteger = 10;

[Output ("ODouble", "Double output")]
var ODouble = 0.1;

[Output ("OString", "String output")]
var OString = "bla bla";

[Output ("ODate", "DateTime output")]
var ODateTime = DateTime.Now;

[Output ("OGuid", "Guid output")]
var OGuid = Guid.NewGuid();

[Output ("OColor", "Color output")]
var OColor = System.Drawing.Color.Azure;

[Output ("OPoint", "Point output")]
var OPoint = new Point3d (0, 1, 2);

//[Output ("OPoint 2d", "Point 2d output")]
//var OPoint2d = new Point3d (0, 1, 2);

[Output ("OVector", "Vector output")]
var OVector = Vector3d.ZAxis;

[Output ("OPlane", "Plane output")]
var OPlane = Plane.WorldXY;

[Output ("OInterval", "Interval output")]
var OInterval = new Interval (0, 1);

[Output ("ORect", "Rectengle output")]
var ORectangel = new Rectangle3d (Plane.WorldXY, 10, 10);

[Output ("OBox", "Box output")]
var OBox = new Box (tPlane, tBrep);

[Output ("OTransform", "Transform output")]
var OTransform = Transform.Scale (tPoint, 2);

tLine.Transform (OTransform);
[Output ("OLine", "Line output")]
var OLine = tLine;

[Output ("OCircle", "Circle output")]
var OCircle = new Circle (10.5);

[Output ("OArc", "Arc output")]
var OArc = new Arc (OCircle, Math.PI);

[Output ("OPLine", "Polyline output")]
var OPolyline = new Polyline (new [] { new Point3d (0, 0, 0), new Point3d (10, 10, 0), new Point3d (0, 10, 10) });

[Output ("OCrv", "Curve output")]
var OCurv = OPolyline;

[Output ("OSurf", "Surface output")]
Surface OSurface;

OSurface = tBrep.Faces[0];

[Output ("OBrep", "Brep output")]
var OBrep = OSurface;

