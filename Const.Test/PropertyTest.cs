using Const.Test.Type;

namespace Const.Test;

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