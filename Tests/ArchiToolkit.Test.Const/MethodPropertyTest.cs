using ArchiToolkit.Test.Const.Type;

namespace ArchiToolkit.Test.Const;

public class BaseMethodTest
{
    public virtual TestClass Property { get; set; } = new();
}

public class MethodPropertyTest : BaseMethodTest
{
    public override TestClass Property { get; set; } = new();
    
    //Coincidence
    [Const(Type = ConstType.All)]
    public void MethodSelfEditTest(int Property)
    {
        Property = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
    }

    [Const(Type = ConstType.Self)]
    public void MethodSelfEditTest()
    {
        Property = new TestClass();
        Property.Sub = new SubClass();
        Property.Sub.Value = 1;
        this.Property = new TestClass();
        this.Property.Sub = new SubClass();
        this.Property.Sub.Value = 1;
        base.Property = new TestClass();
        base.Property.Sub = new SubClass();
        base.Property.Sub.Value = 1;
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
        base.Property = new TestClass();
        base.Property.Sub = new SubClass();
        base.Property.Sub.Value = 1;
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
        base.Property = new TestClass();
        base.Property.Sub = new SubClass();
        base.Property.Sub.Value = 1;
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
        base.Property = new TestClass();
        base.Property.Sub = new SubClass();
        base.Property.Sub.Value = 1;
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
        base.Property = new TestClass();
        base.Property.Sub = new SubClass();
        base.Property.Sub.Value = 1;
    }
}