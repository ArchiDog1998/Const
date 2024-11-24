namespace ArchiToolkit.Test.PropDp;

public partial class SubClass
{
    [PropDp]
    public partial int Item { get; set; }
    
    public void TestMethod(){
    }
}

public partial class PropDpTest
{
    [PropDp]
    public partial SubClass Test { get;  set; }

    [PropDp]
    public partial int AnotherOne { private get; set; }

    [PropDp]
    public partial bool Another { get; }

    private partial bool GetAnother()
    {
        var a = Another;
        Math.Max(AnotherOne, 8);
        var b =this.Test.Item;
        return Test.Item > 0;
    }

    public PropDpTest()
    {
        ClearAnother();

        OnTestChanging += () =>
        {
            Test.OnItemChanged -= ClearAnother;
        };
        OnTestChanged += () =>
        {
            ClearAnother();
            Test.OnItemChanged += ClearAnother;
        };

        Test = new();
    }
    

}
