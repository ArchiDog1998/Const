namespace Const.Test;

public class BaseOverrideTest
{
    public virtual void Test([Const] int value)
    {
        
    }
}

public class OverrideTest : BaseOverrideTest
{
    public override void Test(int value)
    {
        value = 0;
        base.Test(value);
    }
}