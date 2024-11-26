namespace ArchiToolkit.Test.Const;

public class ComplexTest
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }

    [Const]
    public int Test()
    {
        var x = 1;
        var y = 1;

        var up = (A * x + B * y + C).Equals(5);
        return 0;
    }
}