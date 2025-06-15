using Xunit;
using PixelWalle.Interpreter.Runtime;

namespace PixelWalle.Tests;

public class ExecutionStateTests
{
    [Fact]
    public void Should_Store_And_Retrieve_Variables()
    {
        var state = new ExecutionState();
        state.SetVariable("x", 42);
        state.SetVariable("flag", true);
        state.SetVariable("color", "Red");

        Assert.Equal(42, state.GetVariable("x"));
        Assert.Equal(true, state.GetVariable("flag"));
        Assert.Equal("Red", state.GetVariable("color"));
    }

    [Fact]
    public void Should_Throw_If_Getting_Undefined_Variable()
    {
        var state = new ExecutionState();
        Assert.Throws<Exception>(() => state.GetVariable("undefined"));
    }

    [Fact]
    public void Should_Register_Labels()
    {
        var state = new ExecutionState();
        state.DeclareLabel("loop", 5);
        Assert.Equal(5, state.Labels["loop"]);
    }

    [Fact]
    public void Should_Throw_If_Label_Duplicated()
    {
        var state = new ExecutionState();
        state.DeclareLabel("start", 0);
        Assert.Throws<Exception>(() => state.DeclareLabel("start", 2));
    }

    [Fact]
    public void Should_Reset_State()
    {
        var state = new ExecutionState();
        state.SetVariable("x", 1);
        state.DeclareLabel("loop", 3);
        state.BrushColor = "Blue";
        state.BrushSize = 5;
        state.CursorX = 7;
        state.CursorY = 9;

        state.Reset();

        Assert.Empty(state.Variables);
        Assert.Empty(state.Labels);
        Assert.Equal("Black", state.BrushColor);
        Assert.Equal(1, state.BrushSize);
        Assert.Equal(0, state.CursorX);
        Assert.Equal(0, state.CursorY);
    }
    [Fact]
    public void Should_Execute_All_Canvas_Methods()
    {
        var canvas = new MockCanvas();

        canvas.Color("Green");
        canvas.Size(3);
        canvas.DrawLine(1, 1, 5);
        canvas.DrawRectangle(2, 2, 10, 4, 6);
        canvas.DrawCircle(3, 3, 5);
        canvas.Fill();
    }
    
}