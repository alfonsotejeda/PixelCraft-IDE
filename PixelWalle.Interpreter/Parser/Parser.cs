using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Errors;

namespace PixelWalle.Interpreter.Parser;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }
    
    public ProgramNode ParseProgram()
    {
        while (Check(TokenType.NewLine))
            Advance();
        
        var statements = new List<AstNode>();
        while (!IsAtEnd())
        {
            while (Check(TokenType.NewLine))
                Advance();
            if (!IsAtEnd())
                statements.Add(ParseStatement());
        }

        return new ProgramNode(statements, statements[0].Line, statements[0].Column);
    }
    private AstNode ParseStatement()
    {
        if (Check(TokenType.Spawn))
            return ParseSpawn();
        if (IsFunctionName(Current.Type))
            return ParseFunctionCall();
        
        if (Check(TokenType.Identifier))
        {
            if (Peek().Type == TokenType.Assign)
                return ParseAssignment();
            else if (Peek().Type == TokenType.NewLine || Peek().Type == TokenType.LeftBracket || Peek().Type == TokenType.EndOfFile)
                return ParseLabel();
            else
                throw new ParserException(
                    "Se esperaba '<-' para una asignación o salto de línea para una etiqueta.",
                    Current.Line,
                    Current.Column
                );
        }

        if (Check(TokenType.GoTo))
            return ParseGoto();

        if (IsFunctionName(Current.Type))
            return ParseFunctionCall();

        throw new ParserException(
            $"Instrucción no válida al inicio de línea: '{Current.Lexeme}'",
            Current.Line,
            Current.Column
        );
    }
    private AssignmentNode ParseAssignment()
    {
        var nameToken = Consume(TokenType.Identifier, "Se esperaba un nombre de variable");
        var assignToken = Consume(TokenType.Assign, "Se esperaba el operador '<-' para asignación");

        var expr = ParseExpression();

        return new AssignmentNode(nameToken.Lexeme!, expr, nameToken.Line, nameToken.Column);
    }
    private Token Peek()
    {
        if (_position + 1 >= _tokens.Count)
            return _tokens[^1]; // Último token (EOF)
        return _tokens[_position + 1];
    }
    private bool IsFunctionName(TokenType type)
    {
        return type switch
        {
            TokenType.Color or
                TokenType.Fill or
                TokenType.Size or
                TokenType.DrawLine or
                TokenType.DrawCircle or
                TokenType.DrawRectangle or
                TokenType.GetActualX or
                TokenType.GetActualY or
                TokenType.GetCanvasSize or
                TokenType.GetColorCount or
                TokenType.IsBrushColor or
                TokenType.IsBrushSize or
                TokenType.IsCanvasColor => true,
            _ => false,
        };
    }
    public AstNode ParseExpression()
    {
        return ParseOr();
    }
    private AstNode ParseOr()
    {
        var expr = ParseAnd();
        while (Match(TokenType.Or))
        {
            var op = Previous;
            var right = ParseAnd();
            expr = new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParseAnd()
    {
        var expr = ParseComparison();
        while (Match(TokenType.And))
        {
            var op = Previous;
            var right = ParseComparison();
            expr = new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParseComparison()
    {
        var expr = ParseTerm();
        while (Match(TokenType.Equal, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous;
            var right = ParseTerm();
            expr = new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParseTerm()
    {
        var expr = ParseFactor();
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous;
            var right = ParseFactor();
            expr = new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParseFactor()
    {
        var expr = ParsePower();
        while (Match(TokenType.Times, TokenType.Divide, TokenType.Modulo))
        {
            var op = Previous;
            var right = ParsePower();
            expr = new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParsePower()
    {
        var expr = ParsePrimary();
        if (Match(TokenType.Power))
        {
            var op = Previous;
            var right = ParsePower(); // Recursión derecha
            return new BinaryExpressionNode(expr, op.Type, right, op.Line, op.Column);
        }
        return expr;
    }
    private AstNode ParsePrimary()
    {
        var token = Current;

        // Paréntesis
        if (Match(TokenType.LeftParenthesis))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParenthesis, "Se esperaba ')' para cerrar la expresión");
            return expr;
        }

        // Booleanos
        if (Match(TokenType.Boolean))
        {
            bool value = token.Lexeme == "true";
            return new LiteralNode(value, token.Line, token.Column);
        }

        // Números
        if (Match(TokenType.Number))
        {
            if (!int.TryParse(token.Lexeme, out var value))
                throw new ParserException($"Número inválido: {token.Lexeme}", token.Line, token.Column);

            return new LiteralNode(value, token.Line, token.Column);
        }

        // Strings
        if (Match(TokenType.String))
        {
            return new LiteralNode(token.Lexeme, token.Line, token.Column);
        }

        // Funciones o variables
        if (IsFunctionName(Current.Type))
        {
            var funcToken = Advance();
            return ParseFunctionCallFromIdentifier(funcToken);
        }
        else if (Match(TokenType.Identifier))
        {
            return new VariableNode(token.Lexeme!, token.Line, token.Column);
        }
        Console.WriteLine($"[DEBUG] Primary recibió: {Current}");

        throw new ParserException($"Expresión inesperada: '{token.Lexeme}'", token.Line, token.Column);
    }
    private AstNode ParseFunctionCallFromIdentifier(Token functionToken)
    {
        Consume(TokenType.LeftParenthesis, "Se esperaba '(' en la llamada a función");

        var args = new List<AstNode>();
        if (!Check(TokenType.RightParenthesis))
        {
            do
            {
                args.Add(ParseExpression());
            }
            while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParenthesis, "Se esperaba ')' para cerrar los argumentos");

        return new FunctionCallNode(functionToken.Lexeme!, args, functionToken.Line, functionToken.Column);
    }
    private LabelNode ParseLabel()
    {
        var labelToken = Consume(TokenType.Identifier, "Se esperaba un nombre de etiqueta");

        // Validación opcional: evitar nombres inválidos
        if (labelToken.Lexeme!.Any(char.IsWhiteSpace))
            throw new ParserException("Las etiquetas no pueden contener espacios.", labelToken.Line, labelToken.Column);

        return new LabelNode(labelToken.Lexeme!, labelToken.Line, labelToken.Column);
    }
    private GotoNode ParseGoto()
    {
        var gotoToken = Consume(TokenType.GoTo, "Se esperaba 'GoTo'");
        Consume(TokenType.LeftBracket, "Se esperaba '[' después de 'GoTo'");

        var labelToken = Consume(TokenType.Identifier, "Se esperaba el nombre de la etiqueta destino");
        Consume(TokenType.RightBracket, "Se esperaba ']' después del nombre de la etiqueta");

        Consume(TokenType.LeftParenthesis, "Se esperaba '(' antes de la condición");
        var condition = ParseExpression();
        Consume(TokenType.RightParenthesis, "Se esperaba ')' al final de la condición");

        return new GotoNode(labelToken.Lexeme!, condition, gotoToken.Line, gotoToken.Column);
    }
    private SpawnNode ParseSpawn() 
    {
        // Se espera: Spawn ( <number> , <number> )
        var spawnToken = Consume(TokenType.Spawn, "Se esperaba la palabra clave 'Spawn'");
        Consume(TokenType.LeftParenthesis, "Se esperaba '(' después de 'Spawn'");

        var xToken = Consume(TokenType.Number, "Se esperaba un número entero para la coordenada X");
        Consume(TokenType.Comma, "Se esperaba ',' entre las coordenadas");
        var yToken = Consume(TokenType.Number, "Se esperaba un número entero para la coordenada Y");
        Consume(TokenType.RightParenthesis, "Se esperaba ')' después de las coordenadas");

        // Convertimos los lexemas en enteros reales
        if (!int.TryParse(xToken.Lexeme, out int x))
            throw new ParserException($"Coordenada X inválida: '{xToken.Lexeme}'", xToken.Line, xToken.Column);
        if (!int.TryParse(yToken.Lexeme, out int y))
            throw new ParserException($"Coordenada Y inválida: '{yToken.Lexeme}'", yToken.Line, yToken.Column);

        return new SpawnNode(x, y, spawnToken.Line, spawnToken.Column);
    }
    private FunctionCallNode ParseFunctionCall()
    {
        var funcToken = Advance(); // Consumo el nombre de la función

        var name = funcToken.Lexeme!;
        Consume(TokenType.LeftParenthesis, "Se esperaba '(' después de la función");

        var arguments = new List<AstNode>();

        if (!Check(TokenType.RightParenthesis))
        {
            do
            {
                arguments.Add(ParseExpression());
            }
            while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParenthesis, "Se esperaba ')' al final de los argumentos");

        return new FunctionCallNode(name, arguments, funcToken.Line, funcToken.Column);
    }

    
    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];
    private Token Previous => _tokens[_position - 1];
    private bool IsAtEnd() => Current.Type == TokenType.EndOfFile;
    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Previous;
    }
    private bool Check(TokenType type)
    {
        return !IsAtEnd() && Current.Type == type;
    }
    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }
    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new ParserException(message, Current.Line, Current.Column);
    }
}