namespace PixelWalle.Interpreter.AST;

public class FunctionCallNode : AstNode
{
    public string Name { get; }
    public List<AstNode> Arguments { get; }

    public FunctionCallNode(string name, List<AstNode> arguments, int line, int column)
        : base(line, column)
    {
        Name = name;
        Arguments = arguments;
    }

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Arguments)})";
    }
}