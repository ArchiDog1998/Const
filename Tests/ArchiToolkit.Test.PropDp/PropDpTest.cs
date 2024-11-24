namespace ArchiToolkit.Test.PropDp;

public partial class PropDpTest
{
    [PropDp]
    public partial bool Test { get;  set; }

    [PropDp]
    public partial int AnotherOne { private get; set; }

    [PropDp]
    public partial bool Another { get; }

    private partial bool GetAnother()
    {
        this.AnotherOne = 12;
        return true;
    }

    public PropDpTest()
    {
        ClearAnother();
        _Another = new(GetAnother);
        OnTestChanged += ClearAnother;
    }
}
