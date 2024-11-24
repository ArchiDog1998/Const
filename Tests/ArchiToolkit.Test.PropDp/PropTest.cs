namespace ArchiToolkit.Test.PropDp;

internal partial class SubTest
{
    [PropDp]
    public partial int Test { get; set; }
}

internal partial class PropTest
{
    [PropDp]
    public partial SubTest Test { get; set; }
    
    [PropDp]
    public partial int Add { get; }

    private partial int GetAdd()
    {
        return Test.Test + 1;
    }
}
