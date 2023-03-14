namespace Borz;

public class Bindable<T>
{
    public event Action<T>? OnChange;
    
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            OnChange?.Invoke(value);
        }
    }
    
    public Bindable(T value)
    {
        _value = value;
    }
}