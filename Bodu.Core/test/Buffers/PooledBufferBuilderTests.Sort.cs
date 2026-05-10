// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Sort.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort()"/> sorts the written elements in ascending order
    /// using the default comparer.
    /// </summary>
    [TestMethod]
    public void Sort_WhenCalledWithDefaultComparer_ShouldSortWrittenElementsAscending()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([5, 1, 4, 2, 3]);

        builder.Sort();

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort(IComparer{T})"/> sorts the written elements using
    /// the supplied comparer.
    /// </summary>
    [TestMethod]
    public void Sort_WhenCalledWithExplicitComparer_ShouldSortUsingThatComparer()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 3, 5, 2, 4]);

        builder.Sort(Comparer<int>.Create((a, b) => b.CompareTo(a))); // descending

        CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort(Comparison{T})"/> sorts the written elements using
    /// the supplied comparison delegate.
    /// </summary>
    [TestMethod]
    public void Sort_WhenCalledWithComparison_ShouldSortUsingThatComparison()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([3, 1, 4, 1, 5]);

        builder.Sort((a, b) => a.CompareTo(b));

        CollectionAssert.AreEqual(new[] { 1, 1, 3, 4, 5 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort(Comparison{T})"/> with a <see langword="null"/>
    /// comparison throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Sort_WhenComparisonIsNull_ShouldThrowArgumentNullException()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            builder.Sort((Comparison<int>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort()"/> does not affect elements beyond
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void Sort_WhenCalled_ShouldNotChangeWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([3, 1, 2]);

        builder.Sort();

        Assert.AreEqual(3, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort()"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void Sort_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Sort();
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort(Comparison{T})"/> throws
    /// <see cref="ObjectDisposedException"/> after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void Sort_WhenDisposed_ShouldThrowObjectDisposedException_UsingComparison()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Sort((a, b) => a.CompareTo(b));
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort()"/> on an empty builder completes without throwing.
    /// </summary>
    [TestMethod]
    public void Sort_WhenBuilderIsEmpty_ShouldSucceedWithoutError()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.Sort(); // should not throw
        Assert.AreEqual(0, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort()"/> on a single-element builder completes without
    /// throwing and preserves the element.
    /// </summary>
    [TestMethod]
    public void Sort_WhenSingleElement_ShouldSucceedAndPreserveElement()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(42);

        builder.Sort();

        Assert.AreEqual(1, builder.WrittenCount);
        Assert.AreEqual(42, builder.WrittenSpan[0]);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Sort(IComparer{T})"/> with a <see langword="null"/> comparer
    /// argument uses the default comparer and sorts correctly.
    /// </summary>
    [TestMethod]
    public void Sort_WhenExplicitComparerIsNull_ShouldUseDefaultComparerAndSortAscending()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([3, 1, 2]);

        builder.Sort((Comparer<int>?)null);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, builder.WrittenSpan.ToArray());
    }
}
