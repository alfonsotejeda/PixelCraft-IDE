using Xunit;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.AST;
using System;

namespace PixelWalle.Tests;

public class ParserTests
{
    [Fact]
    public void Parser_ShouldParseFullProgramCorrectly()
    {
        string code = string.Join('\n', new[]
        {
            "Spawn(0,0)",
            "Color(\"Red\")",
            "x <- 3 + 2 * GetActualX()",
            "loop-1",
            "DrawLine(1, 0, 5)",
            "GoTo [loop-1] (x < 10)"
        });

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();

        var parser = new Parser(tokens);
        Console.WriteLine($"Primer token: {tokens.First()}");
        var program = parser.ParseProgram();

        Assert.NotNull(program);
        Assert.IsType<ProgramNode>(program);
        Assert.Equal(6, program.Statements.Count);

        // Imprimir el AST para inspección visual
        Console.WriteLine(AstPrinter.Print(program));
    }
}