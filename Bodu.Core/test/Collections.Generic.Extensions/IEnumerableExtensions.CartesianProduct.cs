// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.CartesianProduct.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_CartesianProduct
{
    /// <summary>
    /// Verifies that <c>CartesianProduct</c> pairs every element of the first sequence with every element of the
    /// second, ordered by first position and then second position.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenBothNonEmpty_ShouldReturnAllPairsInOrder()
    {
        (int First, string Second)[] result = new[] { 1, 2 }.CartesianProduct(new[] { "a", "b" }).ToArray();

        CollectionAssert.AreEqual(new[] { (1, "a"), (1, "b"), (2, "a"), (2, "b") }, result);
    }

    /// <summary>
    /// Verifies that the projection overload receives the first-sequence element and then the second-sequence element.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenProjecting_ShouldPassFirstThenSecond()
    {
        string[] result = new[] { 'A', 'B' }
            .CartesianProduct(new[] { 1, 2 }, (column, row) => $"{column}{row}")
            .ToArray();

        CollectionAssert.AreEqual(new[] { "A1", "A2", "B1", "B2" }, result);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> returns an empty sequence when the first input is empty.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenFirstIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().CartesianProduct(new[] { "a", "b" }));

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> returns an empty sequence when the second input is empty.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenSecondIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(new[] { 1, 2 }.CartesianProduct(Array.Empty<string>()));

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> defers enumeration of both inputs until the result is iterated.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenResultNotEnumerated_ShouldNotEnumerateInputs()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().CartesianProduct(Source());

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> enumerates the second input exactly once across the whole product,
    /// replaying its buffer for each first-sequence element, and disposes both enumerators.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenEnumerated_ShouldEnumerateSecondOnceAndDisposeBoth()
    {
        var first = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });
        var second = new DisposeTrackingEnumerable<string>(new[] { "a", "b" });

        var result = first.CartesianProduct(second).ToArray();

        Assert.AreEqual(6, result.Length);
        Assert.AreEqual(1, first.GetEnumeratorCallCount);
        Assert.AreEqual(1, first.DisposeCallCount);
        Assert.AreEqual(1, second.GetEnumeratorCallCount);
        Assert.AreEqual(1, second.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> never enumerates the second input when the first input is empty.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenFirstIsEmpty_ShouldNotEnumerateSecond()
    {
        var second = new DisposeTrackingEnumerable<string>(new[] { "a" });

        _ = Array.Empty<int>().CartesianProduct(second).ToArray();

        Assert.AreEqual(0, second.GetEnumeratorCallCount);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> throws <see cref="ArgumentNullException" /> when the first input is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenFirstIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> first = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = first.CartesianProduct(new[] { "a" });
        });

        Assert.AreEqual("first", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> throws <see cref="ArgumentNullException" /> when the second input is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenSecondIsNull_ShouldThrowExactly()
    {
        IEnumerable<string> second = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1 }.CartesianProduct(second);
        });

        Assert.AreEqual("second", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>CartesianProduct</c> throws <see cref="ArgumentNullException" /> when the result selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CartesianProduct_WhenResultSelectorIsNull_ShouldThrowExactly()
    {
        Func<int, string, string> resultSelector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1 }.CartesianProduct(new[] { "a" }, resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }
}
