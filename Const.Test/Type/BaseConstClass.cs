namespace Const.Test.Type;

public class BaseConstClass
{
    public void NothingMethod()
    {

    }

    [Const(Type = ConstType.Self | ConstType.Members | ConstType.MembersInMembers)]
    public void SelfStrictMethod()
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
    
    [Const(Type = ConstType.Members |  ConstType.MembersInMembers)]
    public void MembersStrictMethod()
    {

    }

    [Const(Type = ConstType.MembersInMembers)]
    public void MembersInMembersMethod()
    {

    }
}