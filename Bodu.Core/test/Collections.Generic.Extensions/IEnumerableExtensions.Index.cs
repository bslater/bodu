// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Index.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NET9_0_OR_GREATER

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Index
{
    /// <summary>
    /// Verifies that <c>Index</c> pairs each element with its zero-based position, in order.
    /// </summary>
    [TestMethod]
    public void Index_WhenInvoked_ShouldPairEachElementWithItsPosition()
    {
        (int Index, string Item)[] result = new[] { "a", "b", "c" }.Index().ToArray();

        CollectionAssert.AreEqual(
            new[] { (0, "a"), (1, "b"), (2, "c") },
            result);
    }

    /// <summary>
    /// Verifies that <c>Index</c> returns an empty sequence for an empty source.
    /// </summary>
    [TestMethod]
    public void Index_WhenSourceIsEmpty_ShouldReturnEmptySequence() => Assert.AreEqual(0, Array.Empty<int>().Index().Count());

    /// <summary>
    /// Verifies that <c>Index</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Index_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        var enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Index();

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Index</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Index_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Index();
        });
    }
}

#endif
