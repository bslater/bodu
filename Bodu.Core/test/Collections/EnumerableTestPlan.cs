// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableTestPlan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections;

/// <summary>
/// Represents a typed test plan for recursive or iterator-based methods.
/// </summary>
/// <typeparam name="TSource">The input sequence element type.</typeparam>
public class EnumerableTestPlan<TSource>
{

    public EnumerableTestPlan(string name, IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable> invoke, IEnumerable<object> expectedResult, Func<TSource, object>? resultSelector = null)
    {
        Name = name;
        Source = source;
        Invoke = invoke;
        ExpectedResult = expectedResult;
        ResultSelector = resultSelector;
    }

    /// <summary>
    /// The expected transformed actual.
    /// </summary>
    public IEnumerable<object> ExpectedResult { get; }

    /// <summary>
    /// The strongly-typed transformation or query logic to test.
    /// </summary>
    public Func<IEnumerable<TSource>, IEnumerable> Invoke { get; }
    public string Name { get; }

    /// <summary>
    /// The strongly-typed selector used to transform results into comparable values.
    /// </summary>
    public Func<TSource, object> ResultSelector { get; }

    /// <summary>
    /// The strongly-typed source input sequence.
    /// </summary>
    public IEnumerable<TSource> Source { get; }

    public override string ToString() => Name;

}
