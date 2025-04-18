using System.Text.RegularExpressions;

namespace PixelWalle.Interpreter.Lexer;

public class LexRule
{
    public Regex Pattern { get; }
    public TokenType Type { get; }
    public Func<string, TokenType>? CustomResolver { get; }

    public LexRule(string pattern, TokenType type, RegexOptions options = RegexOptions.Compiled, Func<string, TokenType>? resolver = null)
    {
        Pattern = new Regex($"^{pattern}", options);
        Type = type;
        CustomResolver = resolver;
    }
}
