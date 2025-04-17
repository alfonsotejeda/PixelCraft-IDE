namespace PixelWalle.Interpreter.Errors;

public abstract class BaseInterpreterException : Exception
{
    public int Line { get; }
    public int Column { get; }

    protected BaseInterpreterException(string message, int line, int column) 
        : base($"{message} (línea {line}, columna {column})")
    {
        Line = line;
        Column = column;
    }
}