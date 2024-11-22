namespace ArchiToolkit.Test.Const.Type;

public class TestClass : BaseConstClass
{
    public int Value { get; set; }

    public SubClass Sub { get; set; } = new();

    public static void Try()
    {
        
    }
}
