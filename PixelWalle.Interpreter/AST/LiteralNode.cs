namespace PixelWalle.Interpreter.AST;

public class LiteralNode : AstNode
{
    public object Value { get; }

    public LiteralNode(object value, int line, int column)
        : base(line, column)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString() ?? "null";
}