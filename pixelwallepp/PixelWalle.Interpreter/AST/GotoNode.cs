namespace PixelWalle.Interpreter.AST;

public class GotoNode : AstNode
{
    public string TargetLabel { get; }
    public AstNode Condition { get; }

    public GotoNode(string targetLabel, AstNode condition, int line, int column)
        : base(line, column)
    {
        TargetLabel = targetLabel;
        Condition = condition;
    }

    public override string ToString()
    {
        return $"Goto({TargetLabel} if {Condition}) at ({Line}, {Column})";
    }
}