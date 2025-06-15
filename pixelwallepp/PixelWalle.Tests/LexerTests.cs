using Xunit;
using PixelWalle.Interpreter.Lexer;
using System.Collections.Generic;
using System.Linq;
using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.Runtime;
using PixelWalle.Interpreter.Semantic;

namespace PixelWalle.Tests
{
    public class LexerTests
    {
        [Fact]
        public void Tokenize_CompleteExample_ShouldReturnAllExpectedTokens()
        {
            string source = @"
Spawn(5,5 )
Color(""Blue"")
DrawLine(0,0,1)
DrawCircle(0,0,3)
";

            // Lexer → Parser → Semantic → Interpreter → Canvas2D
            var tokens = new Lexer(source).TokenizeWithRegex();
            var program = new Parser(tokens).ParseProgram();
            Console.WriteLine(AstPrinter.Print(program));
            var semantic = new SemanticAnalyzer();
            semantic.Analyze(program);

            var state = new ExecutionState();
            var canvas = new Canvas2D(10, 10, state);
            new Interpreter.Runtime.Interpreter(state, canvas).Execute(program);
        
            Console.WriteLine(canvas.DebugView());
            // Verificaciones directas de colores
            // Assert.Equal("Red", canvas.GetColorAt(2, 2));
            // Assert.Equal("Red", canvas.GetColorAt(3, 2));
            // Assert.Equal("Red", canvas.GetColorAt(4, 2));
            // Assert.Equal("Blue", canvas.GetColorAt(5, 3));
            //
            // // Verificación lógica
            // int yellowCount = canvas.GetColorCount("Yellow", 0, 0, 9, 9);
            // Assert.True(yellowCount > 0);

            // Vista visual
        }
        [Fact]
        public void Tokenize_InvalidKeyword_ShouldThrowSuggestionError()
        {
            string source = "Colr(\"Red\")"; // typo en "Color"
            var lexer = new Lexer(source);

            var ex = Assert.Throws<LexerException>(() => lexer.TokenizeWithRegex());
            Assert.Contains("¿Quizás quisiste escribir 'Color'", ex.Message);
        }
        [Fact]
        public void Tokenize_InvalidIdentifierStartingWithDigit_ShouldThrow()
        {
            string source = "1abc";
            var lexer = new Lexer(source);

            var ex = Assert.Throws<LexerException>(() => lexer.TokenizeWithRegex());
            Assert.Contains("Un identificador no puede comenzar con un número: ", ex.Message);
        }
    }
}
