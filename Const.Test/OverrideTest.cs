namespace Const.Test;

public class BaseOverrideTest
{
    [Const]
    public virtual void Test([Const] int value)
    {
        
    }
}

public class OverrideTest : BaseOverrideTest
{
    public int Value { get; set; }
    
    public override void Test(int value)
    {
        value = 0;
        base.Test(value);
        Value = value;
    }
}