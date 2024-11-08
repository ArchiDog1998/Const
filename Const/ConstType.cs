namespace Const;

/// <summary>
/// The type of const.
/// </summary>
[Flags]
public enum ConstType : byte
{
    /// <summary>
    /// Nothing to const.
    /// In this case, why do you use this <see cref="ConstAttribute"/>
    /// </summary>
    None,
    
    /// <summary>
    /// Can't modify the parameter itself.
    /// </summary>
    Self = 1 << 0,
    
    /// <summary>
    /// Can't modify the fields and properties of this parameter.
    /// </summary>
    Members = 1 << 1,
    
    /// <summary>
    /// Can't modify the fields and properties of the fields and properties of this parameter.
    /// </summary>
    MembersInMembers = 1 << 2,
    
    /// <summary>
    /// You can't modify anything.
    /// </summary>
    All = Self | Members | MembersInMembers,
}
