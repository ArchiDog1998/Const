using System.Diagnostics.Contracts;
using System.Numerics;

namespace ArchiToolkit.Test.Const;

public class PureTest
{
    public static List<int> Type { get; set; }
    
    [Const]
    [Pure]
    public static void TestMethod(Vector2 i)
    {
        i.X = 10;
        var a = Math.PI;
        Type.Clear();
    }
}