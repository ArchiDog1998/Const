using Const.Test.Type;

namespace Const.Test;

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