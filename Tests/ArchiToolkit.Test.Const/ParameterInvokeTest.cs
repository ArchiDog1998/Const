using ArchiToolkit.Test.Const.Type;

namespace ArchiToolkit.Test.Const;

public class ParameterInvokeTest
{
    public void Test([Const(Type = ConstType.MembersInMembers)] TestClass item)
    {
        item = new();
        item.NothingMethod();
        item.SelfStrictMethod();
        item.MembersStrictMethod();
    }
}