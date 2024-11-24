namespace ArchiToolkit.Analyzer;

public readonly struct PropDpName(string name)
{
    public string Name => name;
    public string OnNameChanged => $"On{Name}Changed";
    public string OnNameChanging => $"On{Name}Changing";
    public string ClearName => "Clear" + Name;
    public string GetName => "Get" + Name;
    public string LazyName => "_" + Name;
}