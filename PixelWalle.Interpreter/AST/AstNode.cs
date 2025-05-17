namespace PixelWalle.Interpreter.AST;

public abstract class AstNode
{
    public int Line { get; }
    public int Column { get;}

    protected AstNode(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"{GetType().Name} at ({Line}, {Column})";
    }
}