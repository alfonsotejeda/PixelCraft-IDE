namespace PixelWalle.Interpreter.AST;

public class SpawnNode : AstNode
{
    public int X { get; }
    public int Y { get; }

    public SpawnNode(int x, int y, int line, int column)
        : base(line, column)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"SpawnNode({X}, {Y}) at ({Line}, {Column})";
    }
}