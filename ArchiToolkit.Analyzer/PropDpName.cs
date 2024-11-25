namespace ArchiToolkit.Analyzer;

public readonly struct PropDpName(string name)
{
    public string Name => name;
    public string NameChanged => $"{Name}Changed";
    public string NameChanging => $"{Name}Changing";
    public string ClearName => "Clear" + Name;
    public string GetName => "Get" + Name;
    public string SetName => "Set" + Name;
    public string ModifyName => "Modify" + Name;
    public string LazyName => "_" + Name;
    public string InitName => "Init" + Name;
}