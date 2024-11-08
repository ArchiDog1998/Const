using Const.Test.Type;

namespace Const.Test;

public static class ParameterTest
{
    public static void SelfEditTest([Const(Type = ConstType.Self)] TestClass item, int it,
        [Const] int increasement)
    {
        item = new TestClass();
        item.Sub = new SubClass();
        item.Sub.Value = 1;
        it = 10;
        increasement++;
        increasement--;
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
}