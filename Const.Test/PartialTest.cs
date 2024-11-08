namespace Const.Test.Type;

public partial class PartialTest
{
    private partial void DoSomething([Const]string param1);
}

partial class PartialTest
{
    private partial void DoSomething(string param1)
    {
        param1 = "Nice";
    }
}