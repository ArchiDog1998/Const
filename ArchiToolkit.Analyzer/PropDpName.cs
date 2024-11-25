namespace ArchiToolkit.Analyzer;

public readonly struct PropDpName(string name)
{
    public string Name => name;
    public string NameChanged => $"{Name}Changed";
    public string NameChanging => $"{Name}Changing";
    public string ClearName => "_Clear" + Name;
    public string GetName => "_Get" + Name;
    public string SetName => "_Set" + Name;
    public string ModifyName => "_Modify" + Name;
    public string LazyName => "_" + Name;
    public string InitName => "_Init" + Name;
}