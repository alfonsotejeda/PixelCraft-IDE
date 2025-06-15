using PixelWalle.Interpreter.Errors;
namespace PixelWalle.Interpreter.Semantic;

public class SemanticException : BaseInterpreterException
{
    public SemanticException(string message, int line, int column) : 
        base(message, line, column) {}
}