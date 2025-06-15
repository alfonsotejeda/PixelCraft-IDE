using Xunit;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.Semantic;
using PixelWalle.Interpreter.AST;

public class SemanticAnalyzerTests
{
    [Fact]
    public void Should_Throw_When_Spawn_Is_Not_First()
    {
        string code = @"
Color(""Red"")
Spawn(0, 0)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();

        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));
        Assert.Contains("Spawn", ex.Message);
    }

    [Fact]
    public void Should_Pass_When_Spawn_Is_First_And_Unique()
    {
        string code = @"
Spawn(0, 0)
Color(""Red"")
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();

        // No lanza excepción
        analyzer.Analyze(program);
    }
    [Fact]
    public void Should_Throw_When_Using_Undeclared_Variable()
    {
        string code = @"
Spawn(0, 0)
Color(""Red"")
DrawLine(x, 0, 1)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));

        Assert.Contains("Variable 'x' no declarada", ex.Message);
    }
    [Fact]
    public void Should_Allow_Declared_Variable()
    {
        string code = @"
Spawn(0, 0)
x <- 5
DrawLine(x, 0, 1)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();

        analyzer.Analyze(program); // No lanza excepción
    }
    [Fact]
    public void Should_Throw_If_Goto_Label_Is_Undefined()
    {
        string code = @"
Spawn(0, 0)
GoTo [loop] (1 == 1)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));

        Assert.Contains("Etiqueta 'loop' no definida", ex.Message);
    }
    [Fact]
    public void Should_Pass_When_Goto_Label_Exists()
    {
        string code = @"
Spawn(0, 0)
loop
GoTo [loop] (1 == 1)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        analyzer.Analyze(program); // No lanza error
    }
    [Fact]
    public void Should_Throw_On_Type_Mismatch()
    {
        string code = @"
Spawn(0, 0)
x <- 3 + true
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));
        
        Console.WriteLine(AstPrinter.Print(program));

        Assert.Contains("Operación 'Plus' inválida", ex.Message);
    }
    [Fact]
    public void Should_Throw_If_Goto_Condition_Is_Not_Boolean()
    {
        string code = @"
Spawn(0, 0)
loop
GoTo [loop] (3 + 2)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));

        Assert.Contains("condición en 'GoTo' debe ser booleana", ex.Message);
    }
    [Fact]
    public void Should_Allow_Valid_Expression_And_Condition()
    {
        string code = @"
Spawn(0, 0)
x <- 3 + 2
loop
GoTo [loop] (x < 10)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        analyzer.Analyze(program);
    }
    [Fact]
    public void Should_Pass_With_Correct_Function_Call()
    {
        string code = @"
Spawn(0, 0)
DrawLine(1, 0, 3)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        analyzer.Analyze(program);
    }
    [Fact]
    public void Should_Throw_If_Function_Arg_Type_Is_Invalid()
    {
        string code = @"
Spawn(0, 0)
Size(""grande"")
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));

        Assert.Contains("debe ser de tipo Int", ex.Message);
    }
    [Fact]
    public void Should_Throw_If_Function_Has_Wrong_Arg_Count()
    {
        string code = @"
Spawn(0, 0)
DrawLine(1, 2)
";

        var lexer = new Lexer(code);
        var tokens = lexer.TokenizeWithRegex();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        var analyzer = new SemanticAnalyzer();
        var ex = Assert.Throws<SemanticException>(() => analyzer.Analyze(program));

        Assert.Contains("espera 3 argumentos", ex.Message);
    }
}