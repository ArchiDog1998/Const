namespace Const;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
public class ConstAttribute : Attribute
{
    public ConstType Type { get; set; } = ConstType.All;
}
