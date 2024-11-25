using System.Numerics;

namespace ArchiToolkit.Test.PropDp;

internal partial class SubTest
{
    [PropDp]
    public partial Vector2 Test { get; set; }
}

internal partial class PropTest
{
    [PropDp]
    public partial SubTest Test { get; set; }
    
    [PropDp]
    public partial int Add { get; }

    private partial int GetAdd() => SetValue(SetValue(SetValue((int)(Test.Test.X + Test.Test.Y)))) ;

    [Const]
    private int GetValue() => (int)Test.Test.X + 1;

    [Const]
    private int SetValue(int value) => value;
}
