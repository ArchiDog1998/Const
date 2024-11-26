namespace ArchiToolkit.Analyzer;

public readonly struct PropDpName(string name)
{
    public const string Prefix = "_";
    public string Name => name;
    public string NameChanged => $"{Name}Changed";
    public string NameChanging => $"{Name}Changing";
    public string ClearName => Prefix + "Clear" + Name;
    public string GetName => Prefix + "Get" + Name;
    public string SetName => Prefix + "Set" + Name;
    // public string ModifyName => Prefix + "Modify" + Name;
    public string LazyName => "_" + Name;
    public string InitName => Prefix + "Init" + Name;
}