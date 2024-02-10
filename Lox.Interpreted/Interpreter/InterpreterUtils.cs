using System.Numerics;
using System.Text;

namespace Lox.Interpreter;

/// <summary>
/// A collection of helper functions that can be used by other classes.
/// </summary>
internal static class InterpreterUtils
{

    /// <summary>
    /// Try to convert an <see cref="object"/> to a numeric type.
    /// </summary>
    /// <typeparam name="T">The target numeric type.</typeparam>
    /// <param name="p">The input object.</param>
    /// <returns><paramref name="p"/> converted to <typeparamref name="T"/>, if such conversion is possible.</returns>
    /// <exception cref="Exception">If it is not possible to convert <paramref name="p"/> to type <typeparamref name="T"/>.</exception>
    internal static T ToNum<T>(object p) where T : INumber<T>
    {
        static T TryParseString(string s)
        {
            if (!T.TryParse(s, null, out T? res))
            {
                throw new Exception($"Can't convert '{s}' to number!");
            }
            return res;
        }

        if (p is T num)
        {
            return num;
        }

        if (p is string s)
        {
            return TryParseString(s);
        }

        string? str = p.ToString();

        if (str is not null)
        {
            return TryParseString(str);
        }

        throw new Exception($"Can't convert '{p}' to number!");
    }

    /// <summary>
    /// Create a string that shows how many arguments does a given <see cref="ILoxCallable"/> takes.
    /// The string will be in the format '(var, var, ..., var)', where the number of 'var's is equal to the arity of the function.
    /// </summary>
    /// <returns>A string, showing the number of arguments a callable takes.</returns>
    internal static string GetVariableString(int arity)
    {
        StringBuilder varBuilder = new();
        varBuilder.Append('(');
        varBuilder.Append(String.Join(", ", Enumerable.Repeat("var", arity)));
        varBuilder.Append(')');
        return varBuilder.ToString();
    }
}