namespace Const.Test.Type;

public class BaseConstClass
{
    private void NothingMethod()
    {

    }

    [Const(Type = ConstType.Self)]
    private void SelfMethod()
    {

    }

    [Const(Type = ConstType.Members)]
    private void MembersMethod()
    {

    }

    [Const(Type = ConstType.MembersInMembers)]
    private void MembersInMembersMethod()
    {

    }
}