using Xunit;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.Semantic;
using PixelWalle.Interpreter.Runtime;
using PixelWalle.Interpreter.AST;

namespace PixelWalle.Tests.RunTimeTest;

public class Canvas2DIntegrationTest
{
    [Fact]
    public void Should_Execute_FullProgram_And_Reflect_CorrectCanvasState()
    {
        string source = @"
Spawn(2, 2)
Color(""Red"")
DrawLine(1, 0, 3)
Color(""Blue"")
DrawCircle(1,1,3)
DrawLine(0, 1, 3)
Color(""Green"")
DrawRectangle(1, 1, 1, 3, 3)
Color(""Yellow"")
Fill()
x <- 4
DrawLine(1, 0, x)
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
        // // Verificaciones directas de colores
        // Assert.Equal("Red", canvas.GetColorAt(2, 2));
        // Assert.Equal("Red", canvas.GetColorAt(3, 2));
        // Assert.Equal("Red", canvas.GetColorAt(4, 2));
        // Assert.Equal("Blue", canvas.GetColorAt(5, 3));
        
        // // Verificación lógica
        // int yellowCount = canvas.GetColorCount("Yellow", 0, 0, 9, 9);
        // Assert.True(yellowCount > 0);

        // Vista visual
       
    }
    [Fact]
    public void Should_Draw_SimpleFace_On_LargeCanvas()
    {
        string source = @"
Spawn(10, 10)
Color(""White"")
Fill()

Color(""Blue"")
DrawCircle(1, 1, 5)

";
                                                

        var tokens = new Lexer(source).TokenizeWithRegex();
        var program = new Parser(tokens).ParseProgram();
        var semantic = new SemanticAnalyzer();
        Console.WriteLine(AstPrinter.Print(program));
        semantic.Analyze(program);

        var state = new ExecutionState();
        var canvas = new Canvas2D(40, 40, state);
        new Interpreter.Runtime.Interpreter(state, canvas).Execute(program);

        Console.WriteLine(canvas.DebugView());

        // // Validaciones clave
        // Assert.Equal("Yellow", canvas.GetColorAt(20, 20)); // Centro de la cara
        // Assert.Equal("Black", canvas.GetColorAt(12, 12));  // Ojo izquierdo (aproximado)
        // Assert.Equal("Black", canvas.GetColorAt(26, 12));  // Ojo derecho (aproximado)
        // Assert.Equal("Red", canvas.GetColorAt(20, 27));    // Boca (aproximado)
        //
        // int eyePixels = canvas.GetColorCount("Black", 0, 0, 39, 39);
        // int mouthPixels = canvas.GetColorCount("Red", 0, 0, 39, 39);
        // int facePixels = canvas.GetColorCount("Yellow", 0, 0, 39, 39);
        //
        // Assert.True(eyePixels > 0);
        // Assert.True(mouthPixels > 0);
        // Assert.True(facePixels > 0);
    }
}