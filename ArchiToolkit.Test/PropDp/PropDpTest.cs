namespace ArchiToolkit.Test.PropDp;

public partial class PropDpTest
{
    [PropDp]
    public partial bool Test { get; private set; }
    
    [PropDp]
    public partial bool Another { get; }

    private partial bool GetAnother()
    {
        return Test;
    }

    public partial bool Test
    {
        get;
        private set
        {
            if (field.Equals(value)) return;
            field = value;
            OnTestChanged?.Invoke();
        }
    }

    public event Action? OnTestChanged;

    public PropDpTest()
    {
        _Another = new Lazy<bool>(GetAnother);
        OnTestChanged += ClearAnother;
    }
    
    private Lazy<bool> _Another;
    public partial bool Another => _Another.Value;

    private void ClearAnother()
    {
        _Another = new Lazy<bool>(GetAnother);
        OnAnotherChanged?.Invoke();
    }

    public event Action? OnAnotherChanged; 
    private partial bool GetAnother();
}
