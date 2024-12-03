using System.Collections.ObjectModel;
using System.Numerics;

namespace ArchiToolkit.Test.PropDp;

internal partial class SubTest
{
    public event Action? TestEvent;
    
    [FieldDp] public partial float X { get; set; }

    [FieldDp(Comparer = typeof(MyComparer))]
    public partial int Y { get; set; }

    [PropDp] public partial Vector2 Test { get; }

    private partial Vector2 _GetTest()
    {
        int[] test = { 1, 2, 3 };
        test[1] = 10;
        TestEvent?.Invoke();
        var result = new Vector2(X, Y);
        return result;
    }
}

class MyComparer() : IEqualityComparer<int>
{
    public bool Equals(int x, int y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(int obj)
    {
        throw new NotImplementedException();
    }
}

internal partial class PropTest
{
    [FieldDp] public partial ObservableCollection<int> Collection { get; set; }
    [PropDp] public partial ObservableCollection<int> Collection2 { get; }

    private partial ObservableCollection<int> _GetCollection2() => Collection;

    [FieldDp] public partial SubTest Test { get; set; }

    [PropDp] public partial SubTest TestRef { get; set; }

    partial void _SetTestRef(SubTest value)
    {
       
    }

    private partial SubTest _GetTestRef() 
    {
        Test.GetHashCode();
        return new();
    }

    [PropDp] public partial int Add { get; set; }

    partial void _SetAdd(int value)
    {
        this.Test.X = value;
    }

    private partial int _GetAdd() => SetValue(SetValue(SetValue((int)(Test.Test.X + Test.Test.Y))));

    [Const]
    private int GetValue() => (int)Test.Test.X + 1;

    [Const]
    private int SetValue(int value) => value;
}