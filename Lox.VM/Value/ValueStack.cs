using System.Collections;

namespace LoxVM.Value;

/// <summary>
/// Stack that holds <see cref="LoxValue"/>s.
/// </summary>
internal class ValueStack : IEnumerable<LoxValue>
{
    internal LoxValue this[int index]
    {
        get => _stack[index];
        set => _stack[index] = value;
    }

    private readonly int _size;
    private readonly LoxValue[] _stack;

    private int stackTop;

    public ValueStack(int maxSize)
    {
        _size = maxSize;
        _stack = new LoxValue[_size];

        stackTop = 0;
    }

    /// <summary>
    /// Push a <see cref="LoxValue"/> to the top of the stack.
    /// </summary>
    /// <param name="value">The value to be pushed to the top of the stack.</param>
    internal void Push(LoxValue value) => _stack[stackTop++] = value;

    /// <summary>
    /// Remove and return the <see cref="LoxValue"/> on the top of the stack.
    /// </summary>
    /// <returns>The <see cref="LoxValue"/> on the top of the stack.</returns>
    internal LoxValue Pop() => _stack[--stackTop];

    /// <summary>
    /// Return (but do not remove) the <see cref="LoxValue"/> at the specified distance. 
    /// The distance is 0 based, where 0 is the top of the stack. 
    /// </summary>
    /// <param name="distance">The distance from the top of the stack.</param>
    /// <returns>The element at the specified distance.</returns>
    internal LoxValue Peek(int distance) => _stack[stackTop - 1 - distance];

    public IEnumerator<LoxValue> GetEnumerator() => new ValueStackEnumerator(_stack, stackTop);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class ValueStackEnumerator : IEnumerator<LoxValue>
{
    public LoxValue Current => _values[position];

    object IEnumerator.Current => Current;

    private readonly LoxValue[] _values;
    private readonly int _stackTop;

    private int position;

    public ValueStackEnumerator(LoxValue[] values, int top)
    {
        _values = values;
        _stackTop = top;
        position = -1;
    }

    public void Dispose() {}

    public bool MoveNext()
    {
        position++;
        return position < _stackTop;
    }

    public void Reset() => position = -1;
}