using System;

namespace PixelWalle.Interpreter.Errors;

public class InterpreterException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public InterpreterException(string message, int line, int column)
        : base($"Error en tiempo de ejecución (línea {line}, columna {column}): {message}")
    {
        Line = line;
        Column = column;
    }
}