namespace PixelWalle.Interpreter.AST;

public class LabelNode : AstNode
{
    public string Name { get; }

    public LabelNode(string name, int line, int column)
        : base(line, column)
    {
        Name = name;
    }

    public override string ToString() => $"Label({Name}) at ({Line}, {Column})";
}