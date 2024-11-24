using ArchiToolkit.Test.Const.Type;

namespace ArchiToolkit.Test.Const;

public class PropertyTest
{
    public TestClass Property { get; set; } = new();

    public int I
    {
        [Const]
        get
        {
            Property = new();
            return 1;
        }
        private set
        {
            
        }
    }

}