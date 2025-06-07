using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
namespace PixelWalle.Interpreter.Runtime;

public class InterpreterClass
{
    private readonly ExecutionState _state;
    private readonly ICanvas _canvas;

    public InterpreterClass(ExecutionState state, ICanvas canvas)
    {
        _state = state;
        _canvas = canvas;
    }

    public void Execute(ProgramNode program)
    {
        // Registrar etiquetas antes de ejecutar
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is LabelNode label)
            {
                _state.DeclareLabel(label.Name, i);
            }
        }

        int index = 0;
        while (index < program.Statements.Count)
        {
            var stmt = program.Statements[index];

            index++; // Avanzar por defecto

            switch (stmt)
            {
                case SpawnNode spawn:
                    ExecuteSpawn(spawn);
                    break;

                case AssignmentNode assign:
                {
                    var value = Evaluate(assign.Expression);
                    _state.SetVariable(assign.VariableName, value);
                    break;
                }
                
            }
        }
    }

    private void ExecuteSpawn(SpawnNode node)
    {
        if (_state.Variables.ContainsKey("__spawned"))
        {
            throw new InterpreterException("La instrucción Spawn solo puede usarse una vez.", node.Line, node.Column);
        }

        _state.CursorX = node.X;
        _state.CursorY = node.Y;
        _state.Variables["__spawned"] = true;
    }
    private object Evaluate(AstNode expr)
    {
        switch (expr)
        {
            case LiteralNode lit:
                return lit.Value;

            case VariableNode var:
                return _state.GetVariable(var.Name);

            case BinaryExpressionNode bin:
                var left = Evaluate(bin.Left);
                var right = Evaluate(bin.Right);

                return bin.Operator switch
                {
                    TokenType.Plus       => (int)left + (int)right,
                    TokenType.Minus      => (int)left - (int)right,
                    TokenType.Times      => (int)left * (int)right,
                    TokenType.Divide     => (int)left / (int)right,
                    TokenType.Modulo     => (int)left % (int)right,
                    TokenType.Power      => (int)Math.Pow((int)left, (int)right),
                    TokenType.Equal      => Equals(left, right),
                    TokenType.Less       => (int)left < (int)right,
                    TokenType.Greater    => (int)left > (int)right,
                    TokenType.LessEqual  => (int)left <= (int)right,
                    TokenType.GreaterEqual => (int)left >= (int)right,
                    TokenType.And        => (bool)left && (bool)right,
                    TokenType.Or         => (bool)left || (bool)right,
                    _ => throw new InterpreterException($"Operador binario no soportado: {bin.Operator}", bin.Line, bin.Column)
                };

            default:
                throw new InterpreterException("Expresión inválida", expr.Line, expr.Column);
        }
    }
}