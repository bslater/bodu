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
        builder.AppendRange(new[] { 5, 1, 4, 2, 3 });

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
        builder.AppendRange(new[] { 1, 3, 5, 2, 4 });

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
        builder.AppendRange(new[] { 3, 1, 4, 1, 5 });

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
        builder.AppendRange(new[] { 3, 1, 2 });

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
}
