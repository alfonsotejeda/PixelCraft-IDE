namespace PixelWalle.Interpreter.AST;

public class ProgramNode : AstNode
{
    public List<AstNode> Statements { get; }

    public ProgramNode(List<AstNode> statements, int line = -1, int column = -1)
        : base(line, column)
    {
        Statements = statements;
    }
}