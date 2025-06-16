using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using System.Collections.Generic;
using System.Linq;

namespace PixelWalle.Interpreter.Semantic;

public class SemanticAnalyzer
{
    private readonly SymbolTable symbols = new();
    private readonly LabelTable labels = new();
    private bool spawnFound = false;
    private bool spawnInFirstPosition = false;
    

    private readonly List<SemanticException> _errors = new();

    private static readonly Dictionary<string, FunctionSignature> FunctionSignatures = new()
    {
        ["Color"] = new FunctionSignature("Color", Type.String),
        ["Size"] = new FunctionSignature("Size", Type.Int),
        ["DrawLine"] = new FunctionSignature("DrawLine", Type.Int, Type.Int, Type.Int),
        ["DrawCircle"] = new FunctionSignature("DrawCircle", Type.Int, Type.Int , Type.Int),
        ["DrawRectangle"] = new FunctionSignature("DrawRectangle", Type.Int, Type.Int, Type.Int, Type.Int , Type.Int),
        ["Fill"] = new FunctionSignature("Fill"),
        ["SetCursor"] = new FunctionSignature("SetCursor", Type.Int, Type.Int),
        
        ["GetActualX"] = new FunctionSignature("GetActualX"),
        ["GetActualY"] = new FunctionSignature("GetActualY"),
        ["GetCanvasSize"] = new FunctionSignature("GetCanvasSize"),
        ["GetColorCount"] = new FunctionSignature("GetColorCount",Type.String, Type.Int, Type.Int, Type.Int, Type.Int),
        ["IsBrushColor"] = new FunctionSignature("IsBrushColor", Type.String),
        ["IsBrushSize"] = new FunctionSignature("IsBrushSize", Type.Int),
        ["IsCanvasColor"] = new FunctionSignature("IsCanvasColor", Type.String),
    };

    public void Analyze(ProgramNode program)
    {

        if (program.Statements.Count == 0)
        {
            _errors.Add(new SemanticException("El programa está vacío", 0, 0));
            return; // No hay más que analizar si el programa está vacío
        }

        foreach (AstNode stmt in program.Statements)
        {
            if (stmt is LabelNode label)
            {
                if (labels.IsDeclared(label.Name))
                {
                    _errors.Add(new SemanticException($"La etiqueta '{label.Name}' ya fue definida", label.Line, label.Column));
                }
                labels.Declare(label.Name);
            }
        }
        
        for (int i = 0; i < program.Statements.Count; i++)
        {
            AstNode stmt = program.Statements[i];

            if (stmt is SpawnNode spawn)
            {
                if (spawnFound)
                {
                    _errors.Add(new SemanticException("Solo se permite una instrucción 'Spawn'", spawn.Line, spawn.Column));
                }
                if (i != 0)
                {
                    _errors.Add(new SemanticException("'Spawn' debe estar en la primera línea", spawn.Line, spawn.Column));
                }
                spawnFound = true;
            }

            
            try
            {
                AnalyzeNode(stmt);
            }
            catch (SemanticException ex)
            {
                _errors.Add(ex); // Captura SemanticExceptions lanzadas por AnalyzeNode y añádelas
            }
            catch (Exception ex) // Captura cualquier otra excepción inesperada durante el análisis
            {
                _errors.Add(new SemanticException($"Error interno durante análisis semántico: {ex.Message}", stmt.Line, stmt.Column));
            }
        }

        if (!spawnFound)
        {
            _errors.Add(new SemanticException("Todo programa debe comenzar con 'Spawn'", 0, 0));
        }
    }

    private void AnalyzeNode(AstNode node)
    {
        switch (node)
        {
            case AssignmentNode assign:
                AnalyzeNode(assign.Expression);
                symbols.Declare(assign.VariableName, (global::Type)InferType(assign.Expression));
                break;

            case VariableNode varNode:
                if (!symbols.IsDeclared(varNode.Name))
                {
                    _errors.Add(new SemanticException($"Variable '{varNode.Name}' no declarada antes de su uso", varNode.Line, varNode.Column));
                }
                break;
            case UnaryExpressionNode un:
                AnalyzeNode(un.Operand);
                // Si InferType lanza una excepción, AnalyzeNode la captura y la añade a _errors
                // No necesitamos aquí un try-catch anidado si el objetivo es capturar en el nivel superior de Analyze(ProgramNode)
                break;

            case BinaryExpressionNode bin:
                AnalyzeNode(bin.Left);
                AnalyzeNode(bin.Right);
                // Similarmente, si InferType lanza aquí, se propaga
                break;

            case GotoNode g:
                if (!labels.IsDeclared(g.TargetLabel))
                {
                    _errors.Add(new SemanticException($"Etiqueta '{g.TargetLabel}' no definida", g.Line, g.Column));
                }

                AnalyzeNode(g.Condition);

                if (InferType(g.Condition) != Type.Bool)
                {
                    _errors.Add(new SemanticException("La condición en 'GoTo' debe ser booleana", g.Line, g.Column));
                }
                break;
            
            case FunctionCallNode f:
                if (!FunctionSignatures.TryGetValue(f.Name, out FunctionSignature sig))
                {
                    _errors.Add(new SemanticException($"Función '{f.Name}' no reconocida", f.Line, f.Column));
                    return; // No podemos seguir validando argumentos si la función no se reconoce
                }

                if (f.Arguments.Count != sig.ParameterTypes.Length)
                {
                    _errors.Add(new SemanticException($"La función '{f.Name}' espera {sig.ParameterTypes.Length} argumentos, pero se encontraron {f.Arguments.Count}", f.Line, f.Column));
                }

                for (int i = 0; i < f.Arguments.Count; i++)
                {
                    AnalyzeNode(f.Arguments[i]); // Analiza el argumento

                    // Es crucial manejar aquí que InferType podría devolver Type.Error o lanzar si el argumento en sí tiene un problema.
                    // Para evitar múltiples errores en cascada por el mismo problema, puedes hacer una verificación básica.
                    // Sin embargo, si InferType lanza, el try-catch de Analyze(ProgramNode) lo capturará.
                    try
                    {
                        Type argType = InferType(f.Arguments[i]);
                        Type expectedType = sig.ParameterTypes[i];

                        if (argType != expectedType)
                        {
                            _errors.Add(new SemanticException($"El argumento {i + 1} de '{f.Name}' debe ser de tipo {expectedType}, pero se recibió {argType}", f.Arguments[i].Line, f.Arguments[i].Column));
                        }
                    }
                    catch (SemanticException ex)
                    {
                        _errors.Add(ex); // Asegurarse de que las excepciones de InferType se añadan
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(new SemanticException($"Error interno al inferir tipo de argumento para '{f.Name}': {ex.Message}", f.Arguments[i].Line, f.Arguments[i].Column));
                    }
                }
                break;

            case SpawnNode spawn:
                // Spawn ya se maneja en el bucle principal de Analyze para asegurar su posición y unicidad
                // No hay nada más que analizar directamente en el nodo Spawn aparte de eso.
                break;
            case LiteralNode _:
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
                if (!symbols.IsDeclared(v.Name))
                {

                    _errors.Add(new SemanticException($"Variable '{v.Name}' no declarada antes de su uso", v.Line, v.Column));
                    return Type.Error; // Retorna un tipo de error para permitir la continuación del análisis
                }
                return (Type)symbols.GetType(v.Name);
            
            case UnaryExpressionNode un:
                Type operandType = InferType(un.Operand);
                if (operandType == Type.Error) return Type.Error; // Propagar error

                if (operandType != Type.Int)
                {
                    _errors.Add(new SemanticException($"El operador unario '{un.Operator}' solo es válido para enteros", un.Line, un.Column));
                    return Type.Error;
                }
                return Type.Int;

            case BinaryExpressionNode bin:
                Type leftType = InferType(bin.Left);
                Type rightType = InferType(bin.Right);
                if (leftType == Type.Error || rightType == Type.Error) return Type.Error; // Propagar error

                TokenType op = bin.Operator;
                return InferBinaryType(op, leftType, rightType, bin.Line, bin.Column);

            case FunctionCallNode f:
                if (!FunctionSignatures.TryGetValue(f.Name, out FunctionSignature sig))
                {
                    _errors.Add(new SemanticException($"Función '{f.Name}' no reconocida", f.Line, f.Column));
                    return Type.Error; // Retorna tipo de error
                }

                return f.Name switch
                {
                    "GetActualX" => Type.Int,
                    "GetActualY" => Type.Int,
                    "GetCanvasSize" => Type.Int,
                    "GetColorCount" => Type.Int,
                    "IsBrushColor" => Type.Bool, 
                    "IsBrushSize" => Type.Bool,
                    "IsCanvasColor" => Type.Bool,
                    "Color" => Type.Void,
                    "Size" => Type.Void,
                    "DrawLine" => Type.Void,
                    "DrawRectangle" => Type.Void,
                    "Fill" => Type.Void,
                    _ => Type.Error // Para funciones sin tipo de retorno explícito o no reconocidas
                };

            default:
                _errors.Add(new SemanticException($"Tipo de nodo AST no reconocido para inferencia de tipo: {node.GetType().Name}", node.Line, node.Column));
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
            {
                _errors.Add(new SemanticException($"Operación '{op}' inválida entre {left} y {right}", line, column));
                return Type.Error;
            }
        }

        if (op is TokenType.Equal or TokenType.Less or TokenType.Greater or TokenType.LessEqual or TokenType.GreaterEqual)
        {
            if (left == right)
                return Type.Bool;
            else
            {
                _errors.Add(new SemanticException($"Comparación inválida entre {left} y {right}", line, column));
                return Type.Error;
            }
        }

        if (op is TokenType.And or TokenType.Or )
        {
            if (left == Type.Bool && right == Type.Bool)
                return Type.Bool;
            else
            {
                _errors.Add(new SemanticException($"Operación lógica '{op}' requiere booleanos", line, column));
                return Type.Error;
            }
        }

        _errors.Add(new SemanticException($"Operador desconocido: '{op}'", line, column));
        return Type.Error;
    }


    public List<SemanticException> GetErrors()
    {
        return _errors;
    }
}

