namespace PixelWalle.Interpreter.Semantic;

public class FunctionSignature
{
    public string Name { get; }
    public Type[] ParameterTypes { get; }
    
    public FunctionSignature(string name, params Type[] parameterTypes)
    {
        Name = name;
        ParameterTypes = parameterTypes;
    }
}