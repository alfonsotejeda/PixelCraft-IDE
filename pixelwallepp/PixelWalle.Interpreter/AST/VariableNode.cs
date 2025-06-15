namespace PixelWalle.Interpreter.AST;

public class VariableNode : AstNode
{
    public string Name { get; }

    public VariableNode(string name, int line, int column)
        : base(line, column)
    {
        Name = name;
    }

    public override string ToString() => Name;
}