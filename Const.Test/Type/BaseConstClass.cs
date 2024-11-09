namespace Const.Test.Type;

public class BaseConstClass
{
    public void NothingMethod()
    {

    }

    [Const(Type = ConstType.Self)]
    public void SelfMethod()
    {

    }

    [Const(Type = ConstType.Members)]
    public void MembersMethod()
    {

    }

    [Const(Type = ConstType.MembersInMembers)]
    public void MembersInMembersMethod()
    {

    }
}