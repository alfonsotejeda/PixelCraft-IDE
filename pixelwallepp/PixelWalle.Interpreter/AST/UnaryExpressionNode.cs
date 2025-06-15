using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Lexer;

public class UnaryExpressionNode : AstNode
{
    public TokenType Operator { get; }
    public AstNode Operand { get; }

    public UnaryExpressionNode(TokenType op, AstNode operand, int line, int column)
        : base(line, column)
    {
        Operator = op;
        Operand = operand;
    }

    public override string ToString()
        => $"{Operator} {Operand}";
}