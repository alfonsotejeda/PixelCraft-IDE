public class SymbolTable
{
    private readonly Dictionary<string, Type> variables = new();

    public void Declare(string name, Type type)
    {
        variables[name] = type;
    }

    public bool IsDeclared(string name) => variables.ContainsKey(name);

    public Type GetType(string name) => variables[name];
}