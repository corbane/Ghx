using System;

namespace Ghx.RoslynScript
{
    class CompilationException : Exception
    {
        public readonly string Uri;
        public readonly int Line;

        public CompilationException (string message, string uri, int line) : base (message)
        {
            Uri = uri;
            Line = line;
        }

        public CompilationException (Microsoft.CodeAnalysis.Diagnostic diagnostic) : base (diagnostic.GetMessage ())
        {
            var location = diagnostic.Location;
            if( location != Microsoft.CodeAnalysis.Location.None )
                Line = location.GetLineSpan().StartLinePosition.Line;
        }
    }

    class ExecutionException : Exception
    {
        public readonly string Uri;
        public readonly int Line;

        public ExecutionException (string message, string uri, int line) : base (message)
        {
            Uri = uri;
            Line = line;
        }
    }

    class CastExeption : Exception
    {
        public readonly string FieldName;
        public readonly Type FromType;
        public readonly Type ToType;

        public CastExeption (string field, Type fromT, Type toT)
        {
            FieldName = field;
            FromType = fromT;
            ToType = toT;
        }
    }

    class InternalExeption : Exception
    {
        public InternalExeption (string message) : base (message) { }
    }
}
