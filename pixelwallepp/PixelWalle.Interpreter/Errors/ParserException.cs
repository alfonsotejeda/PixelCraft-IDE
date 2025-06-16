namespace PixelWalle.Interpreter.Errors;

public class ParserException : BaseInterpreterException
{
    public ParserException(string message, int line, int column) :
        base(message, line, column)
    {
        
    }
}