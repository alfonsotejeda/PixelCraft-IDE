namespace PixelWalle.Interpreter.Runtime;

public static class DirectionValidator
{
    private static readonly HashSet<(int, int)> ValidDirections = new()
    {
        (-1, -1), (-1, 0), (-1, 1),
        ( 0, -1),  (0,0),  ( 0, 1),
        ( 1, -1), ( 1, 0), ( 1, 1)
    };

    public static void EnsureValid(int dx, int dy, int line, int column)
    {
        if (!ValidDirections.Contains((dx, dy)))
        {
            throw new Errors.InterpreterException(
                $"Dirección inválida: ({dx}, {dy}). Solo se permiten direcciones cardinales o diagonales.",
                line,
                column
            );
        }
    }
}