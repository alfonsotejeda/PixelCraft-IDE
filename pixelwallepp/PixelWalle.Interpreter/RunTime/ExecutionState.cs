using PixelWalle.Interpreter.Errors;
using System.Collections.Generic; // Asegúrate de tener esto

namespace PixelWalle.Interpreter.Runtime;

public class ExecutionState
{
    public Dictionary<string, object> Variables { get; } = new();
    public Dictionary<string, int> Labels { get; } = new();

    public int CursorX { get; set; } = 0;
    public int CursorY { get; set; } = 0;
    public string BrushColor { get; set; } = "Black"; 
    public int BrushSize { get; set; } = 1;


    public int LastExecutedLine { get; set; } = 0;


    public void Reset()
    {
        Variables.Clear();
        Labels.Clear();
        CursorX = 0;
        CursorY = 0;
        BrushColor = "Black";
        BrushSize = 1;

        LastExecutedLine = 0; 
    }

    public void DeclareLabel(string name, int index)
    {
        if (Labels.ContainsKey(name))
            throw new InterpreterException($"Etiqueta '{name}' ya declarada", 0, 0); 
        Labels[name] = index;
    }

    public object GetVariable(string name)
    {
        if (!Variables.ContainsKey(name))
            throw new InterpreterException($"Variable '{name}' no definida", 0, 0);

        return Variables[name];
    }

    public void SetVariable(string name, object value)
    {
        Variables[name] = value;
    }
    public int GetLabelIndex(string label)
    {
        if (!Labels.TryGetValue(label, out int index))
            throw new InterpreterException($"Etiqueta '{label}' no encontrada", 0, 0);
        return index;
    }
}