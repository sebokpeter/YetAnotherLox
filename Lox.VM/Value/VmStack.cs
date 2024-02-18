using System.Collections;

namespace LoxVM.Value;

/// <summary>
/// Stack implementation to hold <see cref="LoxValue"/>s and <see cref="CallFrame"/>s.
/// </summary>
internal class VmStack<T> : IEnumerable<T>
{
    internal T this[int index]
    {
        get => _stack[index];
        set => _stack[index] = value;
    }

    private readonly int _size;
    private readonly T[] _stack;

    private int stackTop;

    public VmStack(int maxSize)
    {
        _size = maxSize;
        _stack = new T[_size];

        stackTop = 0;
    }

    /// <summary>
    /// Push a <typeparamref name="T"/> to the top of the stack.
    /// </summary>
    /// <param name="value">The value to be pushed to the top of the stack.</param>
    internal void Push(T value) => _stack[stackTop++] = value;

    /// <summary>
    /// Remove and return the <typeparamref name="T"/> on the top of the stack.
    /// </summary>
    /// <returns>The <typeparamref name="T"/> on the top of the stack.</returns>
    internal T Pop() => _stack[--stackTop];

    /// <summary>
    /// Return (but do not remove) the <typeparamref name="T"/> at the specified distance. 
    /// The distance is 0 based, where 0 is the top of the stack. 
    /// </summary>
    /// <param name="distance">The distance from the top of the stack.</param>
    /// <returns>The element at the specified distance.</returns>
    internal T Peek(int distance) => _stack[stackTop - 1 - distance];

    public IEnumerator<T> GetEnumerator() => new VmStackEnumerator<T>(_stack, stackTop);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class VmStackEnumerator<T> : IEnumerator<T>
{
    public T Current => _values[position];

    object IEnumerator.Current => Current!;

    private readonly T[] _values;
    private readonly int _stackTop;

    private int position;

    public VmStackEnumerator(T[] values, int top)
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