namespace Const;

/// <summary>
/// Make your method or parameter as const.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
public class ConstAttribute : Attribute
{
    /// <summary>
    /// The way about how to const.
    /// </summary>
    public ConstType Type { get; set; } = ConstType.All;
}
