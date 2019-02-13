#load "./libs.csx"

using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Ghx.RoslynScript;

[Input ("B")]
bool active;

[Input ("L", "Data List")]
List <object> Datas;

if( !(Datas is IEnumerable) )
    throw new Exception ("Input L is not enumerable");

[Output ("R", "result")]
var result = Datas;

// CompilationExeption
// CastExeption (InputCastExeption, OutputCastExeption)
// ExecutorExeption
// ScriptExeption
// ComponentExeption
