using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace PixelWalle.Interpreter.Lexer;

public class Lexer
{
    private string _source;
    private int _column;
    private int _line;
    private int _position;
    private char _currentChar;
    private List<Token> tokens = new List<Token>();
    private static readonly Dictionary<string, TokenType> Instructions = new()
    {
        { "Spawn", TokenType.Spawn },
        { "Color", TokenType.Color },
        { "Size", TokenType.Size },
        { "DrawLine", TokenType.DrawLine },
        { "DrawCircle", TokenType.DrawCircle },
        { "DrawRectangle", TokenType.DrawRectangle },
        { "Fill", TokenType.Fill },
        { "GoTo", TokenType.GoTo }
    };
    private static readonly Dictionary<string, TokenType> Functions = new()
    {
        { "GetActualX", TokenType.GetActualX },
        { "GetActualY", TokenType.GetActualY },
        { "GetCanvasSize", TokenType.GetCanvasSize },
        { "GetColorCount", TokenType.GetColorCount },
        { "IsBrushColor", TokenType.IsBrushColor },
        { "IsBrushSize", TokenType.IsBrushSize },
        { "IsCanvasColor", TokenType.IsCanvasColor }
    };
    private static readonly Dictionary<string, TokenType> Literals = new()
    {
        { "true", TokenType.Boolean },
        { "false", TokenType.Boolean }
    };
    private static readonly List<LexRule> LexRules = new()
    {
        // Palabras clave y funciones: se resolverán dinámicamente por diccionarios
        new LexRule(@"[a-zA-ZñÑ_][a-zA-ZñÑ0-9_-]*", TokenType.Identifier, resolver: ResolveKeywordOrIdentifier),

        // Números enteros
        new LexRule(@"\d+", TokenType.Number),


        // Operadores dobles
        new LexRule(@"\*\*", TokenType.Power),
        new LexRule(@"==", TokenType.Equal),
        new LexRule(@">=", TokenType.GreaterEqual),
        new LexRule(@"<=", TokenType.LessEqual),
        new LexRule(@"<-", TokenType.Assign),
        new LexRule(@"&&", TokenType.And),
        new LexRule(@"\|\|", TokenType.Or),

        // Operadores simples
        new LexRule(@"\+", TokenType.Plus),
        new LexRule("-", TokenType.Minus),
        new LexRule(@"\*", TokenType.Times),
        new LexRule("/", TokenType.Divide),
        new LexRule("%", TokenType.Modulo),
        new LexRule(">", TokenType.Greater),
        new LexRule("<", TokenType.Less),

        // Símbolos
        new LexRule(@"\(", TokenType.LeftParenthesis),
        new LexRule(@"\)", TokenType.RightParenthesis),
        new LexRule(@"\[", TokenType.LeftBracket),
        new LexRule(@"\]", TokenType.RightBracket),
        new LexRule(",", TokenType.Comma),

        // Saltos de línea
        new LexRule(@"\n", TokenType.NewLine),

        // Espacios (pueden omitirse si decides ignorarlos)
        new LexRule(@"[ \t\r]+", TokenType.Whitespace),
        
        // Strings
        new LexRule("\"(?:[^\"\\\\]|\\\\.)*\"", TokenType.String)
    };
    
    public Lexer(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _currentChar = _source[_position];
    }


    public List<Token> Tokenize()
    {
        while (!IsAtEnd())
        {
            switch (_currentChar)
            {
                case ' ':
                case '\t':
                case '\r':
                    Advance();    
                    break;
                case '\n':
                    tokens.Add(new Token("\n", TokenType.NewLine, _line, _column));
                    Advance();
                    break;
                case '+':
                    tokens.Add(new Token("+", TokenType.Plus, _line, _column));
                    Advance();
                    break;
                case '-':
                    tokens.Add(new Token("-", TokenType.Minus, _line, _column));
                    Advance();
                    break;
                case '*':
                    AddDoubleOrSingle('*', "**", TokenType.Power, "*", TokenType.Times); 
                    break;
                case '/':
                    tokens.Add(new Token("/", TokenType.Divide, _line, _column));
                    Advance();
                    break;
                case '%':
                    tokens.Add(new Token("%", TokenType.Modulo, _line, _column));
                    Advance();
                    break;
                case ',':
                    tokens.Add(new Token(",", TokenType.Comma, _line, _column));
                    Advance();
                    break;
                case '=':
                    if (Peek() == '=')
                    {
                        tokens.Add(new Token("==", TokenType.Equal, _line, _column));
                        Advance();
                        Advance();
                    }
                    else
                    {
                        throw new LexerException("Uso inválido de '='. Quizás quisiste escribir '=='", _line, _column);
                    }
                    break;
                case '>':
                    AddDoubleOrSingle('=', ">=", TokenType.GreaterEqual, ">", TokenType.Greater);
                    break;
                case '<':
                    if (Peek() == '=')
                    {
                        tokens.Add(new Token("<=", TokenType.LessEqual, _line, _column));
                        Advance();Advance();
                        break;
                    }
                    else if (Peek() == '-')
                    {
                        tokens.Add(new Token("<-", TokenType.Assign, _line, _column));
                        Advance();Advance();
                        break;
                    }
                    else
                    {
                        tokens.Add(new Token("<", TokenType.Less, _line, _column));
                        Advance();Advance();
                        break;    
                    }
                case '&':
                    if (Peek() == '&')
                    {
                        tokens.Add(new Token("&&", TokenType.And, _line, _column));
                        Advance();
                        Advance();
                    }
                    else
                    {
                        throw new LexerException("Uso inválido de '&'. Quizás quisiste escribir '&&'", _line, _column);
                    }
                    break;
                case '|':
                    if (Peek() == '|')
                    {
                        tokens.Add(new Token("||", TokenType.Or, _line, _column));
                        Advance();
                        Advance();
                    }
                    else
                    {
                        throw new LexerException("Uso inválido de '|'. Quizás quisiste escribir '||'", _line, _column);
                    }
                    break;
                case '(':
                    tokens.Add(new Token("(", TokenType.LeftParenthesis, _line, _column));
                    Advance();
                    break;
                case ')':
                    tokens.Add(new Token(")", TokenType.RightParenthesis, _line, _column));
                    Advance();
                    break;
                case '[':
                    tokens.Add(new Token("[", TokenType.LeftBracket, _line, _column));
                    Advance();
                    break;
                case ']':
                    tokens.Add(new Token("]", TokenType.RightBracket, _line, _column));
                    Advance();
                    break;
                    
                
                default:
                    if (char.IsDigit(_currentChar))
                    {
                        if (!char.IsDigit(Peek()) && char.IsLetter(Peek()))
                        {
                            throw new LexerException($"Un identificador no puede comenzar con un número: '{_currentChar}{Peek()}'", _line, _column);
                        }
                        tokens.Add(ReadNumber());
                    }
                    else if (char.IsLetter(_currentChar))
                    {
                        tokens.Add(ReadWord());
                    }
                    else
                    {
                        throw new LexerException($"Carácter inesperado '{_currentChar}'. No pertenece al alfabeto del lenguaje.", _line, _column);
                    }
                    break;                
            }
            
        }
        
        return tokens;
    }

    public List<Token> TokenizeWithRegex()
    {
        int line = _line;
        int column = _column;

        while (!IsAtEnd())
        {
            string remaining = _source[_position..];
            LexRule? matchedRule = null;
            Match? match = null;
            foreach (var rule in LexRules)
            {
                var m = rule.Pattern.Match(remaining);
                if (m.Success && m.Index == 0)
                {
                    if (match == null || m.Length > match.Length)
                    {
                        matchedRule = rule;
                        match = m;
                    }
                }
            }
            if (match == null || matchedRule == null)
            {
                throw new LexerException($"Carácter inesperado '{_source[_position]}'", line, column);
            }
            string lexeme = match.Value;
            TokenType type = matchedRule.CustomResolver?.Invoke(lexeme) ?? matchedRule.Type;
            // if (matchedRule.CustomResolver != null)
            // {
            //     type = matchedRule.CustomResolver.Invoke(lexeme);
            // }
            // else
            // {
            //     type = matchedRule.Type;
            // }
            
            Console.WriteLine($"LEXEME: '{lexeme}' | TYPE: {type} | Position: {_position} | Source at lookahead: '{(_position + lexeme.Length < _source.Length ? _source[_position + lexeme.Length].ToString() : "EOF")}'");
            //Exceptions
            if (type == TokenType.Number && _position < _source.Length && char.IsLetter(_source[_position]))
            {
                string fragment = _source.Substring(_position - lexeme.Length, lexeme.Length + 1);
                throw new LexerException($"Un identificador no puede comenzar con un número: '{fragment}'", line, column);
            }
            if (type == TokenType.Equal && lexeme == "=")
            {
                throw new LexerException($"Uso inválido de '='. Quizás quisiste escribir '=='", line, column);
            }
            if (type == TokenType.And && lexeme == "&")
            {
                throw new LexerException($"Uso inválido de '&'. Quizás quisiste escribir '&&'", line, column);
            }
            if (type == TokenType.Or && lexeme == "|")
            {
                throw new LexerException($"Uso inválido de '|'. Quizás quisiste escribir '||'", line, column);
            }
            
            int lookahead = _position + lexeme.Length;
            if (type == TokenType.Identifier && lookahead < _source.Length && _source[lookahead] == '(')
            {
                Console.WriteLine($"👉 FUNCION sospechosa: '{lexeme}' en columna {column} línea {line}");
                var sugerencia = SugerenciaCercana(lexeme);
                if (sugerencia != null)
                {
                    throw new LexerException($"Se esperaba una palabra clave o función, pero se encontró '{lexeme}'. ¿Quizás quisiste escribir '{sugerencia}'?", line, column);
                }
                else
                {
                    throw new LexerException($"'{lexeme}' no es una palabra clave reconocida.", line, column);
                }
            }
            
            if (type != TokenType.Whitespace)
            {
                tokens.Add(new Token(lexeme, type, line, column));
            }
            _position += lexeme.Length;
            for (int i = 0; i < lexeme.Length; i++)
            {
                if (lexeme[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
            
        }

        return tokens;
    }

    
    
    

    private void Advance()
    {
        if (_currentChar == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        
        _position++;
        _currentChar = _position < _source.Length ? _source[_position] : '\0';
    }
    private char Peek()
    {
        return _position + 1 < _source.Length ? _source[_position + 1] : '\0';
    }
    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    } 
    private void AddDoubleOrSingle(char expected, string combinedLexeme, TokenType combinedType, string singleLexeme, TokenType singleType)
    {
        if (Peek() == expected)
        {
            tokens.Add(new Token(combinedLexeme, combinedType, _line, _column));
            Advance(); Advance();
        }
        else
        {
            tokens.Add(new Token(singleLexeme, singleType, _line, _column));
            Advance();
        }
    }
    private Token ReadNumber()
    {
        int startColumn = _column;
        var number = new StringBuilder();

        while (char.IsDigit(_currentChar))
        {
            number.Append(_currentChar);
            Advance();
        }

        return new Token(number.ToString(), TokenType.Number, _line, startColumn);
    }
    private Token ReadWord()
    {
        int startColumn = _column;
        var text = new StringBuilder();
        while (char.IsLetter(_currentChar) || char.IsDigit(_currentChar) || _currentChar == '_' || _currentChar == '-')
        {
            text.Append(_currentChar);
            Advance();
        }
        string wordStr = text.ToString();

        if (Instructions.TryGetValue(wordStr, out var tokenType) || Functions.TryGetValue(wordStr, out tokenType) || Literals.TryGetValue(wordStr, out tokenType))
        {
            return new Token(wordStr, tokenType, _line, startColumn);
            
        }
        
        // Paréntesis abierto => palabra reservada
        else if (_currentChar == '(')
        {
            // Buscar sugerencia cercana
            string? sugerencia = null;
            int mejorDistancia = int.MaxValue;

            foreach (string palabraClave in Instructions.Keys.Concat(Functions.Keys).Concat(Literals.Keys))
            {
                int distancia = Levenshtein(wordStr, palabraClave);
                if (distancia <= 2 && distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    sugerencia = palabraClave;
                }
            }

            if (sugerencia != null)
            {
                throw new LexerException($"Se esperaba una palabra clave, pero se encontró '{wordStr}'. ¿Quizás quisiste escribir '{sugerencia}'?", _line, startColumn);
            }
            else
            {
                throw new LexerException($"Palabra clave no reconocida: '{wordStr}'", _line, startColumn);
            }
        }
        // else if (_currentChar == '\n')
        // {
        //     return new Token(wordStr, TokenType.Etiquette, _line, startColumn);
        // }
        return new Token(wordStr, TokenType.Identifier, _line, startColumn);
    }
    private static TokenType ResolveKeywordOrIdentifier(string lexeme)
    {
        if (Instructions.TryGetValue(lexeme, out var instr)) return instr;
        if (Functions.TryGetValue(lexeme, out var func)) return func;
        if (Literals.TryGetValue(lexeme, out var lit)) return lit;
        return TokenType.Identifier;
    }
    private int Levenshtein(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        for (int j = 0; j <= m; j++)
            d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1,      // Eliminación
                        d[i, j - 1] + 1),     // Inserción
                    d[i - 1, j - 1] + cost); // Sustitución
            }
        }

        return d[n, m];
    }
    private string? SugerenciaCercana(string input)
    {
        const int umbral = 2; // distancia máxima aceptada
        string? sugerencia = null;
        int mejorDistancia = int.MaxValue;

        foreach (var palabra in Instructions.Keys.Concat(Functions.Keys).Concat(Literals.Keys))
        {
            int dist = Levenshtein(input, palabra);
            if (dist < mejorDistancia && dist <= umbral)
            {
                mejorDistancia = dist;
                sugerencia = palabra;
            }
        }

        return sugerencia;
    }
   
    
}

