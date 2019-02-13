#load "./libs.csx"

using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Ghx.RoslynScript;

[Input ("1")]
object o1;

[Input ("2")]
object o2;

[Input ("3")]
object o3;

[Input ("4")]
object o4;

var u = 10;

[Output ("O1")]
var O1 = o1;

[Output ("O2")]
var O2 = o2;

[Output ("O3")]
var O3 = o3;

[Output ("O4")]
var O4 = o4;
