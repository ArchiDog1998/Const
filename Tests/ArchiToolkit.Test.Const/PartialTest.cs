namespace ArchiToolkit.Test.Const;

public partial class PartialTest
{
    [Const]
    private partial void DoSomething([Const]string param1);
}

partial class PartialTest
{
    public int Value { get; set; }
    private partial void DoSomething(string param1)
    {
        param1 = "Nice";
        Value = 10;
    }
}