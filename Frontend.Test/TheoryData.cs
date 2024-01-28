using System.Collections;

namespace Frontend.Test;
// From: https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/
public abstract class TheoryData : IEnumerable<object[]>
{
    private readonly List<object[]> data = [];

    protected void AddRow(params object[] values) => data.Add(values);

    public IEnumerator<object[]> GetEnumerator() => data.GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}