using PixelWalle.Interpreter.Lexer;

namespace PixelWalle.Interpreter.AST;

public class BinaryExpressionNode : AstNode
{
    public AstNode Left { get; }
    public TokenType Operator { get; }
    public AstNode Right { get; }

    public BinaryExpressionNode(AstNode left, TokenType op, AstNode right, int line, int column)
        : base(line, column)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}