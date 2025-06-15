namespace PixelWalle.Interpreter.Lexer;

using PixelWalle.Interpreter.Errors;

public class LexerException : BaseInterpreterException
{
    public LexerException(string message, int line, int column)
        : base($"Error léxico: {message}", line, column)
    {
    }
}