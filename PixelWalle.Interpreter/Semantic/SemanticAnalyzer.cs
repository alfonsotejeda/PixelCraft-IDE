using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;

namespace PixelWalle.Interpreter.Semantic;

public class SemanticAnalyzer
{
    private readonly SymbolTable symbols = new();
    private readonly LabelTable labels = new();
    private bool spawnFound = false;
    private bool spawnInFirstPosition = false;
    private static readonly Dictionary<string, FunctionSignature> FunctionSignatures = new()
    {
        ["Color"] = new FunctionSignature("Color", Type.String),
        ["Size"] = new FunctionSignature("Size", Type.Int),
        ["DrawLine"] = new FunctionSignature("DrawLine", Type.Int, Type.Int, Type.Int),
        ["DrawCircle"] = new FunctionSignature("DrawCircle", Type.Int, Type.Int , Type.Int),
        ["DrawRectangle"] = new FunctionSignature("DrawRectangle", Type.Int, Type.Int, Type.Int, Type.Int , Type.Int),
        ["Fill"] = new FunctionSignature("Fill"),
        
        ["GetActualX"] = new FunctionSignature("GetActualX"),
        ["GetActualY"] = new FunctionSignature("GetActualY"),
        ["GetCanvasSize"] = new FunctionSignature("GetCanvasSize"),
        ["GetColorCount"] = new FunctionSignature("GetColorCount",Type.String, Type.Int, Type.Int, Type.Int, Type.Int),
        ["IsBrushColor"] = new FunctionSignature("IsBrushColor", Type.String),
        ["IsBrushSize"] = new FunctionSignature("IsBrushSize", Type.Int),
        ["IsCanvasColor"] = new FunctionSignature("IsCanvasColor", Type.String , Type.Int , Type.Int),
    };

    public void Analyze(ProgramNode program)
    {
        if (program.Statements.Count == 0)
            throw new SemanticException("El programa está vacío", 0, 0);

        foreach (var stmt in program.Statements)
        {
            if (stmt is LabelNode label)
            {
                if (labels.IsDeclared(label.Name))
                {
                    throw new SemanticException($"La etiqueta '{label.Name}' ya fue definida", label.Line, label.Column);
                }

                labels.Declare(label.Name);
            }
        }
        
        for (int i = 0; i < program.Statements.Count; i++)
        {
            var stmt = program.Statements[i];

            if (stmt is SpawnNode spawn)
            {
                if (spawnFound)
                    throw new SemanticException("Solo se permite una instrucción 'Spawn'", spawn.Line, spawn.Column);
                if (i != 0)
                    throw new SemanticException("'Spawn' debe estar en la primera línea", spawn.Line, spawn.Column);
                spawnFound = true;
            }

            AnalyzeNode(stmt);
        }

        if (!spawnFound)
            throw new SemanticException("Todo programa debe comenzar con 'Spawn'", 0, 0);
    }
    private void AnalyzeNode(AstNode node)
    {
        switch (node)
        {
            case AssignmentNode assign:
                // Validar lado derecho (expresión)
                AnalyzeNode(assign.Expression);

                // Registrar variable como declarada
                symbols.Declare(assign.VariableName, InferType(assign.Expression));
                break;

            case VariableNode varNode:
                if (!symbols.IsDeclared(varNode.Name))
                    throw new SemanticException($"Variable '{varNode.Name}' no declarada antes de su uso", varNode.Line, varNode.Column);
                break;

            case BinaryExpressionNode bin:
                AnalyzeNode(bin.Left);
                AnalyzeNode(bin.Right);
                break;

            case GotoNode g:
                if (!labels.IsDeclared(g.TargetLabel))
                    throw new SemanticException($"Etiqueta '{g.TargetLabel}' no definida", g.Line, g.Column);

                AnalyzeNode(g.Condition);

                if (InferType(g.Condition) != Type.Bool)
                    throw new SemanticException("La condición en 'GoTo' debe ser booleana", g.Line, g.Column);
                break;
            
            case FunctionCallNode f:
                if (!FunctionSignatures.TryGetValue(f.Name, out var sig))
                    throw new SemanticException($"Función '{f.Name}' no reconocida", f.Line, f.Column);

                if (f.Arguments.Count != sig.ParameterTypes.Length)
                {
                    throw new SemanticException($"La función '{f.Name}' espera {sig.ParameterTypes.Length} argumentos, pero se encontraron {f.Arguments.Count}", f.Line, f.Column);
                }

                for (int i = 0; i < f.Arguments.Count; i++)
                {
                    AnalyzeNode(f.Arguments[i]);

                    var argType = InferType(f.Arguments[i]);
                    var expectedType = sig.ParameterTypes[i];

                    if (argType != expectedType)
                    {
                        throw new SemanticException($"El argumento {i + 1} de '{f.Name}' debe ser de tipo {expectedType}, pero se recibió {argType}", f.Line, f.Column);
                    }
                }
                break;

            
        }
    }
    private Type InferType(AstNode node)
    {
        switch (node)
        {
            case LiteralNode l:
                return l.Value switch
                {
                    int => Type.Int,
                    bool => Type.Bool,
                    string => Type.String,
                    _ => Type.Error
                };

            case VariableNode v:
                return symbols.GetType(v.Name);

            case BinaryExpressionNode bin:
                var leftType = InferType(bin.Left);
                var rightType = InferType(bin.Right);
                var op = bin.Operator;

                return InferBinaryType(op, leftType, rightType, bin.Line, bin.Column);

            case FunctionCallNode f:
                if (!FunctionSignatures.TryGetValue(f.Name, out var sig))
                    throw new SemanticException($"Función '{f.Name}' no reconocida", f.Line, f.Column);

                // Retornar tipo de retorno según el nombre de la función
                return f.Name switch
                {
                    "GetActualX" => Type.Int,
                    "GetActualY" => Type.Int,
                    "GetCanvasSize" => Type.Int,
                    "GetColorCount" => Type.Int,
                    "IsBrushColor" => Type.Int,
                    "IsBrushSize" => Type.Bool,
                    "IsCanvasColor" => Type.Bool,
                    "Color" => Type.Error,
                    "Size" => Type.Error,
                    "DrawLine" => Type.Error,
                    "DrawRectangle" => Type.Error,
                    "Fill" => Type.Error,
                    _ => Type.Error
                };

            default:
                return Type.Error;
        }
    }
    private Type InferBinaryType(TokenType op, Type left, Type right, int line, int column)
    {
        if (op is TokenType.Plus or TokenType.Minus or TokenType.Times or TokenType.Divide or TokenType.Modulo or TokenType.Power)
        {
            if (left == Type.Int && right == Type.Int)
                return Type.Int;
            else
                throw new SemanticException($"Operación '{op}' inválida entre {left} y {right}", line, column);
        }

        if (op is TokenType.Equal or TokenType.Less or TokenType.Greater or TokenType.LessEqual or TokenType.GreaterEqual)
        {
            if (left == right)
                return Type.Bool;
            else
                throw new SemanticException($"Comparación inválida entre {left} y {right}", line, column);
        }

        if (op is TokenType.And or TokenType.Or )
        {
            if (left == Type.Bool && right == Type.Bool)
                return Type.Bool;
            else
                throw new SemanticException($"Operación lógica '{op}' requiere booleanos", line, column);
        }

        throw new SemanticException($"Operador desconocido: '{op}'", line, column);
    }
}