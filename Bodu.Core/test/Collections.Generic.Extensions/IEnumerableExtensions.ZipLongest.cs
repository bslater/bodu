// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ZipLongest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_ZipLongest
{
    /// <summary>
    /// Verifies that <c>ZipLongest</c> returns an empty sequence when both inputs are empty.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenBothEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().ZipLongest(Array.Empty<string>()));

    /// <summary>
    /// Verifies that <c>ZipLongest</c> behaves like <c>Zip</c> when both inputs have the same length.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenSameLength_ShouldPairElementwise()
    {
        (int First, string Second)[] result = new[] { 1, 2 }.ZipLongest(new[] { "a", "b" }).ToArray();

        CollectionAssert.AreEqual(new[] { (1, "a"), (2, "b") }, result);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> pads the second side with its default once the second input is exhausted.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenFirstIsLonger_ShouldPadSecondWithDefault()
    {
        var result = new[] { 1, 2, 3 }.ZipLongest(new[] { "a" }).ToArray();

        CollectionAssert.AreEqual(new[] { (1, "a"), (2, (string?)null), (3, (string?)null) }, result);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> pads the first side with its default once the first input is exhausted.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenSecondIsLonger_ShouldPadFirstWithDefault()
    {
        (int First, string Second)[] result = new[] { 1 }.ZipLongest(new[] { "a", "b", "c" }).ToArray();

        CollectionAssert.AreEqual(new[] { (1, "a"), (0, "b"), (0, "c") }, result);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> uses the supplied default values when padding an exhausted side.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenExplicitDefaultsSupplied_ShouldUseThemForPadding()
    {
        (int First, string Second)[] result = new[] { 1 }
            .ZipLongest(new[] { "a", "b" }, -1, "?")
            .ToArray();

        CollectionAssert.AreEqual(new[] { (1, "a"), (-1, "b") }, result);
    }

    /// <summary>
    /// Verifies that the projection overload receives first and second values in the correct order.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenProjecting_ShouldPassFirstThenSecond()
    {
        string[] result = new[] { 1, 2, 3 }
            .ZipLongest(new[] { "a" }, 0, "_", (x, y) => $"{x}{y}")
            .ToArray();

        CollectionAssert.AreEqual(new[] { "1a", "2_", "3_" }, result);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> defers enumeration of both inputs until the result is iterated.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenResultNotEnumerated_ShouldNotEnumerateInputs()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().ZipLongest(Source());

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> enumerates each input once and disposes both enumerators, even when the inputs
    /// are of unequal length.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenEnumerated_ShouldEnumerateEachInputOnceAndDisposeBoth()
    {
        var first = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });
        var second = new DisposeTrackingEnumerable<string>(new[] { "a" });

        _ = first.ZipLongest(second).ToArray();

        Assert.AreEqual(1, first.GetEnumeratorCallCount);
        Assert.AreEqual(1, first.DisposeCallCount);
        Assert.AreEqual(1, second.GetEnumeratorCallCount);
        Assert.AreEqual(1, second.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> can be bounded when one input is infinite and the other finite.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenOneInputIsInfinite_ShouldBoundWithTake()
    {
        var result = Naturals()
            .ZipLongest(new[] { "a", "b" })
            .Take(3)
            .ToArray();

        CollectionAssert.AreEqual(new[] { (0, "a"), (1, "b"), (2, (string?)null) }, result);
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> throws <see cref="ArgumentNullException" /> when the first input is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenFirstIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> first = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = first.ZipLongest(new[] { "a" });
        });
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> throws <see cref="ArgumentNullException" /> when the second input is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenSecondIsNull_ShouldThrowExactly()
    {
        IEnumerable<string> second = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1 }.ZipLongest(second);
        });
    }

    /// <summary>
    /// Verifies that <c>ZipLongest</c> throws <see cref="ArgumentNullException" /> when the selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ZipLongest_WhenSelectorIsNull_ShouldThrowExactly()
    {
        Func<int, string, string> selector = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1 }.ZipLongest(new[] { "a" }, 0, "_", selector);
        });
    }

    private static IEnumerable<int> Naturals()
    {
        int i = 0;
        while (true)
            yield return i++;
    }
}
