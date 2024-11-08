using Const.Test.Type;

namespace Const.Test;

internal class TestMaster
{
    public TestClass Property { get; set; } = new();

    [Const(Type = ConstType.Self)]
    private void MethodInvokeTestSelf()
    {
        Property.NothingMethod();
        NothingMethod();
        SelfMethod();
        MembersMethod();
        MembersInMembersMethod();
    }
    
    #region Using Members
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
    #endregion

    //Coincidence
    [Const(Type = ConstType.All)]
    public void MethodSelfEditTest(int Property)
    {
        Property = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    #region Method Test

    [Const(Type = ConstType.Self)]
    public void MethodSelfEditTest()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    [Const(Type = ConstType.Members)]
    public void MethodMemberEditTest()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    [Const(Type = ConstType.MembersInMembers)]
    public void MethodMemberInMemberEditTest()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    [Const(Type = ConstType.All)]
    public void MethodAllEditTest()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    [Const]
    public void MethodAllEditTest2()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }
    #endregion

    #region Parameter Test
    public static void SelfEditTest([Const(Type = ConstType.Self)] TestClass item, int it)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
        it = 10;
    }

    public static void MemberEditTest([Const(Type = ConstType.Members)] TestClass item)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
    }

    public static void MemberInMemberEditTest([Const(Type = ConstType.MembersInMembers)] TestClass item)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
    }

    public static void AllEditTest([Const(Type = ConstType.All)] TestClass item)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
    }

    public static void AllEditTest2([Const] TestClass item)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
    }
    #endregion
}