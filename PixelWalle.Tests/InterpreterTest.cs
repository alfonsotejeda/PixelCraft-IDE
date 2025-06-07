using Xunit;
using PixelWalle.Interpreter.AST;
using PixelWalle.Interpreter.Runtime;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;


namespace PixelWalle.Tests;

public class InterpreterTests
{
    [Fact]
    public void Should_Set_Cursor_On_Spawn()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(5, 7, line: 1, column: 1)
        }, - 1 ,-1);

        interpreter.Execute(program);

        Assert.Equal(5, state.CursorX);
        Assert.Equal(7, state.CursorY);
    }

    [Fact]
    public void Should_Throw_If_Spawn_Called_Twice()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(1, 1, 1, 1),
            new SpawnNode(2, 2, 2, 1)
        } , -1 , -1);

        var ex = Assert.Throws<InterpreterException>(() => interpreter.Execute(program));
        Assert.Contains("solo puede usarse una vez", ex.Message);
    }
    
    [Fact]
    public void Should_Assign_And_Use_Variables()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new AssignmentNode("a", new LiteralNode(3, 1, 1), 1, 1),
            new AssignmentNode("b", new BinaryExpressionNode(
                new VariableNode("a", 1, 2),TokenType.Times ,new LiteralNode(2,1,3), 1, 2), 1, 2)
        });

        interpreter.Execute(program);

        Assert.Equal(3, state.GetVariable("a"));
        Assert.Equal(6, state.GetVariable("b"));
    }
    [Fact]
    public void Should_Evaluate_Expression_And_Print()
    {
        var expr = new BinaryExpressionNode(
            new BinaryExpressionNode(
                new LiteralNode(2, 1, 1),
                TokenType.Plus,
                new LiteralNode(3, 1, 2),
                1, 1
            ),
            TokenType.Times,
            new LiteralNode(4, 1, 3),
             1, 1
        );

        // Print el árbol
        string printed = AstPrinter.Print(expr);
        Console.WriteLine("Árbol de expresión:");
        Console.WriteLine(printed);

        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        // Evaluar el valor
        var result = interpreter.EvaluateForTest(expr);
        Console.WriteLine($"Resultado evaluado: {result}");

        Assert.Equal(20, result); // (2 + 3) * 4 = 20
    }
    [Fact]
    public void Should_Execute_DrawRectangle_As_FunctionCall()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new FunctionCallNode("DrawRectangle", new List<AstNode>
            {
                new LiteralNode(1, 2, 1), // dx
                new LiteralNode(0, 2, 2), // dy
                new LiteralNode(3, 2, 3), // distance
                new LiteralNode(4, 2, 4), // width
                new LiteralNode(5, 2, 5)  // height
            }, 2, 1)
        });

        interpreter.Execute(program);
    }
    [Fact]
    public void Should_Throw_On_Invalid_DrawRectangle_Direction()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new FunctionCallNode("DrawRectangle", new List<AstNode>
            {
                new LiteralNode(2, 3, 1), // ❌ dx inválido
                new LiteralNode(0, 3, 2),
                new LiteralNode(3, 3, 3),
                new LiteralNode(4, 3, 4),
                new LiteralNode(5, 3, 5)
            }, 3, 1)
        });

        var ex = Assert.Throws<InterpreterException>(() => interpreter.Execute(program));
        Assert.Contains("Dirección inválida", ex.Message);
    }
    [Fact]
    public void Should_Execute_DrawCircle_As_FunctionCall()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new FunctionCallNode("DrawCircle", new List<AstNode>
            {
                new LiteralNode(0, 4, 1), // dx
                new LiteralNode(-1, 4, 2), // dy (arriba)
                new LiteralNode(3, 4, 3)   // radius
            }, 4, 1)
        });

        interpreter.Execute(program);
    }
    [Fact]
    public void Should_Throw_On_Invalid_DrawCircle_Direction()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new FunctionCallNode("DrawCircle", new List<AstNode>
            {
                new LiteralNode(2, 4, 1), // ❌ dirección inválida
                new LiteralNode(1, 4, 2),
                new LiteralNode(5, 4, 3)
            }, 4, 1)
        });

        var ex = Assert.Throws<InterpreterException>(() => interpreter.Execute(program));
        Assert.Contains("Dirección inválida", ex.Message);
    }
    [Fact]
    public void Should_Execute_Fill_As_FunctionCall()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new FunctionCallNode("Fill", new List<AstNode>(), 4, 1)
        });

        interpreter.Execute(program);

        Assert.True(canvas.FillCalled); // Supongamos que tu mock registra si fue invocado
    }
    
    
    // [Fact]
    // public void Should_Evaluate_GetActualX_And_Y()
    // {
    //     var state = new ExecutionState();
    //     var canvas = new MockCanvas();
    //     var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);
    //     string code = string.Join('\n', new[]
    //     {
    //         "Spawn(0,0)",
    //         "x <- 3 + 2 * GetActualX()",
    //         "loop-1",
    //         "DrawLine(1, 0, 5)",
    //         "GoTo [loop-1] (x < 10)"
    //     });
    //     
    //     var lexer = new Lexer(code);
    //     var tokens = lexer.TokenizeWithRegex();
    //
    //     var parser = new Parser(tokens);
    //     Console.WriteLine($"Primer token: {tokens.First()}");
    //     var program = parser.ParseProgram(); 
    //
    //
    //     
    //     interpreter.Execute(program);
    //
    //     Assert.Equal(10, state.GetVariable("x"));
    //     Assert.Equal(20, state.GetVariable("y"));
    // }
    
    [Fact]
    public void Interpreter_Should_Jump_To_Label_Using_GoTo()
    {
        var state = new ExecutionState();
        var canvas = new MockCanvas();
        var interpreter = new Interpreter.Runtime.Interpreter(state, canvas);

        var program = new ProgramNode(new List<AstNode>
        {
            new SpawnNode(0, 0, 1, 1),
            new AssignmentNode("i", new LiteralNode(1, 2, 1), 2, 1),
            new GotoNode("salto", new LiteralNode(1, 3, 1), 3, 1),
            new AssignmentNode("i", new LiteralNode(99, 4, 1), 4, 1),
            new LabelNode("salto", 5, 1),
            new AssignmentNode("j", new LiteralNode(10, 6, 1), 6, 1),
        });

        interpreter.Execute(program);

        Assert.Equal(1, state.GetVariable("i"));   // Se ejecutó antes del salto
        Assert.False(state.Variables.ContainsKey("i_ignorada")); // No se ejecutó la i = 99
        Assert.Equal(10, state.GetVariable("j"));  // Después del salto
    }
    
    
}