namespace PixelWalle.Interpreter.AST;

public class ProgramNode : AstNode
{
    public List<AstNode> Statements { get; }

    public ProgramNode(List<AstNode> statements, int line, int column)
        : base(line, column)
    {
        Statements = statements;
    }
}