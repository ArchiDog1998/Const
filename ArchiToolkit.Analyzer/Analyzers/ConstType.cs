namespace ArchiToolkit.Analyzer.Analyzers;

[Flags]
public enum ConstType : byte
{
    None,
    Self = 1 << 0,
    Members = 1 << 1,
    MembersInMembers = 1 << 2,
}