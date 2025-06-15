public class LabelTable
{
    private readonly HashSet<string> labels = new();

    public void Declare(string name) => labels.Add(name);

    public bool IsDeclared(string name) => labels.Contains(name);
}