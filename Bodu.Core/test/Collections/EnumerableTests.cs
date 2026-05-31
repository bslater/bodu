// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Infrastructure;

namespace Bodu.Collections;

public abstract partial class EnumerableTests
{

    /// <summary>
    /// Asserts that the extension method defers execution and does not begin enumeration when invoked.
    /// </summary>
    /// <typeparam name="TSource">The type of input elements.</typeparam>
    /// <param name="methodName">The name of the method being tested (used for assertion messaging).</param>
    /// <param name="invokeExtensionMethod">A delegate that wraps the extension method call.</param>
    protected static void AssertExecutionIsDeferred<TSource>(
        string methodName,
        Func<IEnumerable<TSource>, IEnumerable> invokeExtensionMethod,
        IEnumerable<TSource> values)
    {
        var wasEnumerated = false;

        var source = new TrackingEnumerable<TSource>(
            values,
            onEnumerate: () => wasEnumerated = true
        );

        IEnumerable actual = invokeExtensionMethod(source);

        Assert.IsFalse(wasEnumerated, $"[{methodName}] should defer execution.");
    }

    /// <summary>
    /// Asserts that the extension method triggers enumeration only when the actual is actually enumerated.
    /// </summary>
    /// <typeparam name="TSource">The type of input elements.</typeparam>
    /// <param name="methodName">The name of the method being tested (used for assertion messaging).</param>
    /// <param name="invokeExtensionMethod">A delegate that wraps the extension method call.</param>
    protected static void AssertExecutionOccursOnEnumeration<TSource>(
        string methodName,
        Func<IEnumerable<TSource>, IEnumerable> invokeExtensionMethod,
        IEnumerable<TSource> values)
    {
        var wasEnumerated = false;

        var source = new TrackingEnumerable<TSource>(
            values,
            onEnumerate: () => wasEnumerated = true
        );

        IEnumerable actual = invokeExtensionMethod(source);

        // Trigger enumeration
        _ = actual.GetEnumerator().MoveNext();

        Assert.IsTrue(wasEnumerated, $"[{methodName}] should begin enumeration when enumerated.");
    }

    /// <summary>
    /// Asserts that invoking an extension method on the given source returns the expected results, with optional actual transformation.
    /// </summary>
    /// <typeparam name="TSource">The type of input elements.</typeparam>
    /// <typeparam name="TResult">The type of actual elements.</typeparam>
    /// <param name="methodName">The name of the method under test.</param>
    /// <param name="invokeExtensionMethod">The extension method to invoke on the source.</param>
    /// <param name="source">The input sequence.</param>
    /// <param name="expected">The expected actual sequence.</param>
    /// <param name="selector">
    /// Optional projection to convert elements from the actual to <typeparamref name="TResult" />. If not specified, elements are cast
    /// to <typeparamref name="TResult" />.
    /// </param>
    //protected static void AssertExecutionReturnsExpectedResults<TSource, TResult>(
    //    string methodName,
    //    Func<IEnumerable<TSource>, IEnumerable> invokeExtensionMethod,
    //    IEnumerable<TSource> source,
    //    IEnumerable<TResult> expected,
    //    Func<TSource, TResult>? selector = null)
    //{
    protected static void AssertExecutionReturnsExpectedResults<TSource, TResult>(
        string methodName,
        Func<IEnumerable<TSource>, IEnumerable> invokeExtensionMethod,
        IEnumerable<TSource> source,
        IEnumerable<TResult> expected,
        Func<TSource, TResult> resultSelector)
    {
        // Evaluate actual with optional selector or cast
        var actual = invokeExtensionMethod(source)
            .Cast<object>()
            .Select(e => resultSelector != null ? resultSelector((TSource)e) : (TResult)e)
            .ToList();

        var expectedList = expected.ToList();

        CollectionAssert.AreEqual(expectedList, actual);
    }

}
