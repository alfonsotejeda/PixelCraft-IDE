using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.RunTime;

namespace PixelWalle.Interpreter.Runtime;

public class Interpreter
{
    private readonly ExecutionState _state;
    private readonly ICanvas _canvas;
    private List<AstNode> _nodes; // Contendrá todas las sentencias del programa
    private int _instructionPointer;

    // --- NUEVAS PROPIEDADES PARA EJECUCIÓN PARCIAL ---
    private int _startLine;
    private int _linesToProcess;
    public int LastExecutedLine { get; private set; } // Para devolver la última línea ejecutada

    public Interpreter(ExecutionState state, ICanvas canvas)
    {
        _state = state;
        _canvas = canvas;
        LastExecutedLine = 0; // Inicializar
    }

    // --- Sobrecarga de constructor para ejecución parcial (Godot) ---
    public Interpreter(ExecutionState state, ICanvas canvas, int startLine, int linesToProcess)
    {
        _state = state;
        _canvas = canvas;
        _startLine = startLine;
        _linesToProcess = linesToProcess;
        LastExecutedLine = 0; // Inicializar
    }

    public void Execute(ProgramNode program)
    {
        _nodes = program.Statements;
        
        // Registrar etiquetas antes de ejecutar (SIEMPRE se hace sobre todo el programa)
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is LabelNode label)
            {
                _state.DeclareLabel(label.Name, i);
            }
        }
        
        // --- Modificación clave aquí: Lógica para ejecución parcial ---
        // Si _startLine es 0 o 1, significa que no se ha especificado un rango
        // o se está iniciando la ejecución desde el principio.
        // Si _linesToProcess es -1, significa ejecutar todo.
        
        // Encontrar el punto de inicio real en _nodes basado en _startLine
        _instructionPointer = 0; // Por defecto al principio del programa
        if (_startLine > 1) // Si no es la primera línea, buscar el punto de inicio
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Line >= _startLine)
                {
                    _instructionPointer = i;
                    break;
                }
            }
        }

        int statementsProcessedInChunk = 0;
        int maxStatementsInChunk = _linesToProcess == -1 ? int.MaxValue : _linesToProcess; // Si -1, procesar todo

        while (_instructionPointer < _nodes.Count && statementsProcessedInChunk < maxStatementsInChunk)
        {
            var stmt = _nodes[_instructionPointer];
            
            // Si la línea de la sentencia actual excede el rango de líneas a procesar
            // y no estamos en modo "procesar todo"
            if (_linesToProcess != -1 && stmt.Line >= (_startLine + _linesToProcess))
            {
                // Detenerse aquí. La siguiente sentencia está fuera del chunk.
                // Es importante que la sentencia actual (stmt) *esté* dentro del rango
                // o que sea la primera del chunk y no se exceda el limite por esta sentencia.
                // Esta verificación es un poco simplista. Si una sentencia empieza en línea N
                // y se extiende hasta N+X, y N+X > (_startLine + _linesToProcess),
                // aún así se ejecutaría completa si N está dentro del rango.
                // Para tu lenguaje, asumiendo que las sentencias son relativamente atómicas
                // por línea, esto debería ser suficiente.
                break; 
            }

            bool jumped = false;

            switch (stmt)
            {
                case SpawnNode spawn:
                    ExecuteSpawn(spawn);
                    break;

                case AssignmentNode assign:
                    var value = Evaluate(assign.Expression);
                    _state.SetVariable(assign.VariableName, value);
                    break;

                case FunctionCallNode call:
                    ExecuteFunctionCall(call);
                    break;

                case GotoNode gotoNode:
                    var result = Evaluate(gotoNode.Condition);

                    bool shouldJump = result switch
                    {
                        int i => i != 0,
                        bool b => b,
                        _ => throw new InterpreterException("La condición de GoTo debe ser entera o booleana", gotoNode.Line, gotoNode.Column)
                    };

                    if (shouldJump)
                    {
                        // Cuando hay un salto, el nuevo _instructionPointer debe ser el de la etiqueta.
                        // Luego, *no se incrementa* al final del bucle.
                        // Y necesitamos salir del chunk si el salto va fuera del rango del chunk actual.
                        int targetIndex = _state.GetLabelIndex(gotoNode.TargetLabel);
                        if (_linesToProcess != -1 && (_nodes[targetIndex].Line < _startLine || _nodes[targetIndex].Line >= (_startLine + _linesToProcess)))
                        {
                            // Si el salto está fuera del chunk actual, salta, pero terminamos el chunk.
                            // Esto significa que Godot deberá volver a llamar con el nuevo _startLine.
                            _instructionPointer = targetIndex;
                            jumped = true;
                            // No incrementar statementsProcessedInChunk ya que estamos saliendo del chunk.
                            // Esto terminará la ejecución del chunk actual.
                            break; 
                        }
                        _instructionPointer = targetIndex;
                        jumped = true;
                    }
                    break;

                case LabelNode:
                    // Las etiquetas no hacen nada en sí mismas durante la ejecución.
                    break;

                default:
                    throw new InterpreterException("Nodo no soportado en ejecución", stmt.Line, stmt.Column);
            }

            LastExecutedLine = stmt.Line; // Actualizar la última línea ejecutada
            statementsProcessedInChunk++;

            if (!jumped)
                _instructionPointer++;
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
            case FunctionCallNode call:
            {
                var args = call.Arguments.Select(Evaluate).ToArray();

                switch (call.Name)
                {
                    case "GetActualX":
                        if (args.Length != 0)
                            throw new InterpreterException("GetActualX no acepta argumentos", call.Line, call.Column);
                        return _state.CursorX;

                    case "GetActualY":
                        if (args.Length != 0)
                            throw new InterpreterException("GetActualY no acepta argumentos", call.Line, call.Column);
                        return _state.CursorY;

                    case "IsBrushColor":
                    {
                        if (args.Length != 1)
                            throw new InterpreterException("IsBrushColor espera 1 argumento", call.Line, call.Column);

                        if (args[0] is not string color)
                            throw new InterpreterException("El argumento de IsBrushColor debe ser un string", call.Line, call.Column);

                        return _canvas.IsBrushColor(color) ? 1 : 0;
                    }

                    case "IsBrushSize":
                    {
                        if (args.Length != 1)
                            throw new InterpreterException("IsBrushSize espera 1 argumento", call.Line, call.Column);

                        int size = Convert.ToInt32(args[0]);
                        return _canvas.IsBrushSize(size) ? 1 : 0;
                    }

                    case "IsCanvasColor":
                    {
                        if (args.Length != 1)
                            throw new InterpreterException("IsCanvasColor espera 1 argumento", call.Line, call.Column);

                        if (args[0] is not string canvasColor)
                            throw new InterpreterException("El argumento de IsCanvasColor debe ser un string", call.Line, call.Column);

                        return _canvas.IsCanvasColor(canvasColor) ? 1 : 0;
                    }
                    case "GetCanvasSize":
                    {
                        if (args.Length != 0)
                            throw new InterpreterException("GetCanvasSize no acepta argumentos", call.Line, call.Column);

                        return _canvas.GetCanvasSize();
                    }

                    case "GetColorCount":
                    {
                        if (args.Length != 5)
                            throw new InterpreterException("GetColorCount espera 5 argumentos: color, x1, y1, x2, y2", call.Line, call.Column);

                        if (args[0] is not string color)
                            throw new InterpreterException("El primer argumento de GetColorCount debe ser un string", call.Line, call.Column);

                        int x1 = Convert.ToInt32(args[1]);
                        int y1 = Convert.ToInt32(args[2]);
                        int x2 = Convert.ToInt32(args[3]);
                        int y2 = Convert.ToInt32(args[4]);

                        return _canvas.GetColorCount(color, x1, y1, x2, y2);
                    }
        
                    default:
                        throw new InterpreterException($"Función desconocida en contexto de expresión: {call.Name}", call.Line, call.Column);
                }
            }
            case UnaryExpressionNode un:
                var value = Evaluate(un.Operand);
                return un.Operator switch
                {
                    TokenType.Minus => -(int)value,
                    TokenType.Plus => +(int)value,
                    _ => throw new InterpreterException($"Operador unario no soportado: {un.Operator}", un.Line, un.Column)
                };
            
            default:
                throw new InterpreterException("Expresión inválida", expr.Line, expr.Column);
        }
    }

    public object EvaluateForTest(AstNode expr)
    {
        return Evaluate(expr); // Método interno privado
    }
    private void ExecuteFunctionCall(FunctionCallNode call)
{
    var args = call.Arguments.Select(Evaluate).ToArray();

    switch (call.Name)
    {
        case "DrawRectangle":
        {
            if (args.Length != 5)
                throw new InterpreterException($"DrawRectangle espera 5 argumentos, pero se recibieron {args.Length}", call.Line, call.Column);

            int dx = Convert.ToInt32(args[0]);
            int dy = Convert.ToInt32(args[1]);
            int distance = Convert.ToInt32(args[2]);
            int width = Convert.ToInt32(args[3]);
            int height = Convert.ToInt32(args[4]);

            DirectionValidator.EnsureValid(dx, dy, call.Line, call.Column);
            _canvas.DrawRectangle(dx, dy, distance, width, height);

            // _state.CursorX += dx * distance;
            // _state.CursorY += dy * distance;
            break;
        }

        case "DrawCircle":
        {
            if (args.Length != 3)
                throw new InterpreterException($"DrawCircle espera 3 argumentos, pero se recibieron {args.Length}", call.Line, call.Column);

            int dx = Convert.ToInt32(args[0]);
            int dy = Convert.ToInt32(args[1]);
            int radius = Convert.ToInt32(args[2]);

            DirectionValidator.EnsureValid(dx, dy, call.Line, call.Column);
            _canvas.DrawCircle(dx, dy, radius);

            // _state.CursorX += dx * radius;
            // _state.CursorY += dy * radius;
            break;
        }

        case "DrawLine":
        {
            if (args.Length != 3)
                throw new InterpreterException($"DrawLine espera 3 argumentos, pero se recibieron {args.Length}", call.Line, call.Column);

            int dx = Convert.ToInt32(args[0]);
            int dy = Convert.ToInt32(args[1]);
            int distance = Convert.ToInt32(args[2]);

            DirectionValidator.EnsureValid(dx, dy, call.Line, call.Column);
            _canvas.DrawLine(dx, dy, distance);

            // _state.CursorX += dx * distance;
            // _state.CursorY += dy * distance;
            break;
        }

        case "Color":
        {
            if (args.Length != 1 || args[0] is not string color)
                throw new InterpreterException("Color espera 1 argumento de tipo string", call.Line, call.Column);

            _canvas.Color(color);
            break;
        }

        case "Size":
        {
            if (args.Length != 1)
                throw new InterpreterException("Size espera 1 argumento", call.Line, call.Column);

            int size = Convert.ToInt32(args[0]);
            _canvas.Size(size);
            break;
        }

        case "Fill":
        {
            if (args.Length != 0)
                throw new InterpreterException("La función Fill no acepta argumentos", call.Line, call.Column);

            _canvas.Fill();
            break;
        }
        case "SetCursor":
        {
            if (args.Length != 2)
                throw new InterpreterException("SetCursor espera 2 argumentos", call.Line, call.Column);

            int x = Convert.ToInt32(args[0]);
            int y = Convert.ToInt32(args[1]);

            _canvas.SetCursor(x, y);
            break;
        }

        default:
            throw new InterpreterException($"Función desconocida: {call.Name}", call.Line, call.Column);
    }
}
}