using UnitsNet;

namespace ArchiToolkit.Test.PropDp;

internal partial class Point2
{
    [PropDp]
    public partial Length X { get; set; }
    
    [PropDp]
    public partial Length Y { get; set; }
}

internal partial class Arc2
{
    [PropDp]
    public partial Length Radius { get; set; }
    
    [PropDp]
    public partial Point2 Location { get; set; } = new Point2();

    [PropDp]
    public partial Angle StartAngle { get; set; }
}