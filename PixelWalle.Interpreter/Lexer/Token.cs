namespace PixelWalle.Interpreter.Lexer;

public class Token
{
    public string? Lexeme { get; private set; }
    public TokenType Type { get; private set; }
    public int Line { get; private set; }
    public int Column { get; private set; }

    public Token(string? value, TokenType type, int line, int column)
    {
        this.Lexeme = value;
        this.Type = type;
        this.Line = line;
        this.Column = column;
    }
    
    public override string ToString()
    {
        return $"Token: {Type}, Value: {Lexeme}, Line: {Line}, Column: {Column}";
    }
}



public enum TokenType
{
    // Instructions 
    Spawn,
    Color,
    Size,
    DrawLine,
    DrawCircle,
    DrawRectangle,
    Fill,
    GoTo,
    
    // Indentifiers
    Number,
    Identifier, //or variable name
    // Etiquette,
    Boolean,
    String,
    
    // Operators
    Plus,
    Minus,
    Times,
    Divide,
    Modulo,
    Power,
    Assign,
    
    // Bool operators
    And,
    Or,
    GreaterEqual,
    LessEqual,
    Equal,
    Greater,
    Less,
    
    // Functions
    GetActualX,
    GetActualY,
    GetCanvasSize,
    GetColorCount,
    IsBrushColor,
    IsBrushSize,
    IsCanvasColor,

    // Especial characters 
    LeftParenthesis,
    RightParenthesis,
    LeftBracket,
    RightBracket,
    NewLine,
    Comma,
    Whitespace,
    
    // Special tokens
    EndOfFile,
}