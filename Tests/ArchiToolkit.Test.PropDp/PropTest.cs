namespace ArchiToolkit.Test.PropDp;
internal partial class PropTest
{
    [PropDp]
    public partial int Test { get; set; }
    //public partial int Test { get; set  => field = value; }
    
    [PropDp]
    public partial int Add { get; }

    private partial int GetAdd()
    {
        return Test + 1;
    }
}
