namespace PixelWalle.Interpreter.AST;

public class AssignmentNode : AstNode
{
    public string VariableName { get; }
    public AstNode Expression { get; }

    public AssignmentNode(string name, AstNode expression, int line, int column)
        : base(line, column)
    {
        VariableName = name;
        Expression = expression;
    }

    public override string ToString()
    {
        return $"Assignment({VariableName} <- {Expression}) at ({Line}, {Column})";
    }
}