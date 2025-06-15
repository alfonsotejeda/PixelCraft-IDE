using Xunit;
    using PixelWalle.Interpreter.Parser;
    using PixelWalle.Interpreter.Lexer;
    using PixelWalle.Interpreter.AST;

    namespace PixelWalle.Tests.ParserTests;

    public class UnaryParserTests
    {
        [Fact]
        public void Parser_ShouldParse_NegativeLiteral()
        {
            var source = "a <- -5";
            var lexer = new Lexer(source);
            var tokens = lexer.TokenizeWithRegex();
            var parser = new Parser(tokens);

            var program = parser.ParseProgram();
            var assign = Assert.IsType<AssignmentNode>(program.Statements[0]);

            var unary = Assert.IsType<UnaryExpressionNode>(assign.Expression);
            Assert.Equal(TokenType.Minus, unary.Operator);

            var literal = Assert.IsType<LiteralNode>(unary.Operand);
            Assert.Equal(5, literal.Value);
        }

        [Fact]
        public void Parser_ShouldParse_NegatedParenthesizedExpression()
        {
            var source = "a <- -(3 + 2)";
            var lexer = new Lexer(source);
            var tokens = lexer.TokenizeWithRegex();
            var parser = new Parser(tokens);

            var program = parser.ParseProgram();
            var assign = Assert.IsType<AssignmentNode>(program.Statements[0]);
            var unary = Assert.IsType<UnaryExpressionNode>(assign.Expression);
            var binary = Assert.IsType<BinaryExpressionNode>(unary.Operand);

            Assert.Equal(TokenType.Plus, binary.Operator);
        }
    }