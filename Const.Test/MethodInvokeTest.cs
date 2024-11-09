using Const.Test.Type;

namespace Const.Test;

public class MethodInvokeTest : BaseConstClass
{
    public TestClass Property { get; set; } = new();

    private void MethodInMethodTest([Const(Type = ConstType.Self)]int i)
    {
        [Const(Type = ConstType.Self)]
        void Function([Const(Type = ConstType.Self)]int i)
        {
            i = 10;
            Property.NothingMethod();
            this.NothingMethod();
            NothingMethod();
            SelfMethod();
            MembersMethod();
            MembersInMembersMethod();
        }

        [Const(Type = ConstType.Self)]
        void NothingMethod()
        {
 
        }
    }
    

    [Const(Type = ConstType.Self)]
    private void MethodInvokeTestSelf()
    {
        Property.NothingMethod();
        NothingMethod();
        SelfMethod();
        MembersMethod();
        MembersInMembersMethod();
    }

}