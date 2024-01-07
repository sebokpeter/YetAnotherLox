

namespace LoxConsole.Interpreter;

internal class LoxArray
{
    private List<object> _values;

    internal List<object> Values => _values;

    public LoxArray(List<object> values)
    {
        _values = values;
    }

    internal void Assign(int targetLocation, object value)
    {
        if(targetLocation >= _values.Count) 
        {
            _values.AddRange(new object[targetLocation + 1 - _values.Count]);
        }
        _values[targetLocation] = value;
    }

    internal object Get(int targetLocation, Token bracket)
    {
        if(targetLocation > _values.Count - 1) 
        {
            throw new RuntimeException(bracket, $"Position is out of range. Array size: {_values.Count}, requested position: {targetLocation}");
        }

        return _values[targetLocation];
    }
}