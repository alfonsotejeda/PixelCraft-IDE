using Xunit;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.Semantic;
using PixelWalle.Interpreter.AST;

namespace PixelWalle.Tests;

public class InterpreterTest
{
    [Fact]
    public void Should_Analyze_CompleteProgram_Correctly()
    {
        string code = string.Join('\n', new[]
        {
            "Spawn(0, 0)",
            "Color(\"Black\")",
            "n <- 5",
            "k <- 3 + 3 * 10",
            "n <- k * 2",
            "actual_x <- GetActualX()",
            "i <- 0",
            "",
            "loop1",
            "DrawLine(1, 0, 1)",
            "i <- i + 1",
            "is_brush_color_blue <- IsBrushColor(\"Blue\")",
            "GoTo [loop_ends_here] (is_brush_color_blue == 1)",
            "GoTo [loop1] (i < 10)",
            "",
            "Color(\"Blue\")",
            "GoTo [loop1] (1 == 1)",
            "",
            "loop_ends_here"
        });

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();
        var analyzer = new SemanticAnalyzer();
        
        Console.WriteLine(AstPrinter.Print(program));
        
        analyzer.Analyze(program); // No lanza excepción
    }
    
}