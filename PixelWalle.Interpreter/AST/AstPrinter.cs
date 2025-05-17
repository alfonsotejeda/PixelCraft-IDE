using System.Text;

namespace PixelWalle.Interpreter.AST;

public static class AstPrinter
{
    public static string Print(AstNode node, int indent = 0)
    {
        var sb = new StringBuilder();
        string pad = new string(' ', indent * 2);

        switch (node)
        {
            case ProgramNode program:
                sb.AppendLine($"{pad}Program:");
                foreach (var stmt in program.Statements)
                    sb.Append(Print(stmt, indent + 1));
                break;

            case SpawnNode spawn:
                sb.AppendLine($"{pad}Spawn(x={spawn.X}, y={spawn.Y})");
                break;

            case AssignmentNode assign:
                sb.AppendLine($"{pad}Assign {assign.VariableName} <-");
                sb.Append(Print(assign.Expression, indent + 1));
                break;

            case FunctionCallNode call:
                sb.AppendLine($"{pad}Call {call.Name}(");
                foreach (var arg in call.Arguments)
                    sb.Append(Print(arg, indent + 1));
                sb.AppendLine($"{pad})");
                break;

            case GotoNode gotoNode:
                sb.AppendLine($"{pad}GoTo [{gotoNode.TargetLabel}] if:");
                sb.Append(Print(gotoNode.Condition, indent + 1));
                break;

            case LabelNode label:
                sb.AppendLine($"{pad}Label: {label.Name}");
                break;

            case LiteralNode literal:
                sb.AppendLine($"{pad}Literal: {literal.Value}");
                break;

            case VariableNode variable:
                sb.AppendLine($"{pad}Variable: {variable.Name}");
                break;

            case BinaryExpressionNode bin:
                sb.AppendLine($"{pad}Binary ({bin.Operator})");
                sb.Append(Print(bin.Left, indent + 1));
                sb.Append(Print(bin.Right, indent + 1));
                break;

            default:
                sb.AppendLine($"{pad}Unknown node: {node.GetType().Name}");
                break;
        }

        return sb.ToString();
    }
}