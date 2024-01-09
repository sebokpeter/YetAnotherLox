

namespace LoxConsole.Interpreter;

/// <summary>
/// Runtime representation of a lox array.
/// It is a wrapper around a List<object>, with methods to get and insert values.
/// </summary>
internal class LoxArray
{
    private List<object> _values;

    internal List<object> Values => _values;

    public LoxArray(List<object> values)
    {
        _values = values;
    }

    /// <summary>
    /// Insert <paramref name="value"/> to the location specified by <paramref name="targetLocation"/>. If the value of <paramref name="targetLocation"/> is larger
    /// than the current size of the array, the array will be filled with elements until it's size is at least <paramref name="targetLocation"/>.
    /// </summary>
    /// <param name="targetLocation">The location (index) of the value being inserted.</param>
    /// <param name="value">The value being inserted.</param>
    internal void Assign(int targetLocation, object value)
    {
        // We can add to any location in an array
        if(targetLocation >= _values.Count) 
        {
            // So if the current size of the array is smaller than the position of the new object, we just backfill the array 
            _values.AddRange(new object[targetLocation + 1 - _values.Count]);
        }
        _values[targetLocation] = value;
    }

    /// <summary>
    /// Get the value located at the index specified by <paramref name="targetLocation"/>.
    /// If <paramref name="targetLocation"/> is larger than the size of the array, a <see cref="RuntimeException"> will be thrown.
    /// </summary>
    /// <param name="targetLocation">An index, indicating the location of the requested value.</param>
    /// <param name="bracket">A <see cref="Token"/> that marks the beginning of the array access. It is used to report the location of the array access, in case of an error.</param>
    /// <returns>The element located at index <paramref name="targetLocation"/>.</returns>
    /// <exception cref="RuntimeException">If the index (<paramref name="targetLocation"/>) is larger than the size of the array.</exception>
    internal object Get(int targetLocation, Token bracket)
    {
        if(targetLocation > _values.Count - 1) 
        {
            throw new RuntimeException(bracket, $"Position is out of range. Array size: {_values.Count}, requested position: {targetLocation}");
        }

        return _values[targetLocation];
    }

    public override string ToString() => $"<array {_values.Count}>";
}