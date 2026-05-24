// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Contains.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Contains" /> uses the configured
    /// element comparer when matching case-insensitive variants.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementComparerIsCaseInsensitive_ShouldMatchCaseVariant()
    {
        var queue = new IndexedPriorityQueue<string, int>(0, null, StringComparer.OrdinalIgnoreCase);
        queue.Enqueue("alpha", 1);

        Assert.IsTrue(queue.Contains("ALPHA"));
        Assert.AreEqual(1, queue.GetPriority("AlPhA"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Contains" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.Contains(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Contains" /> returns <see langword="false" /> for a missing element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementMissing_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.IsFalse(queue.Contains("b"));
    }
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Contains" /> returns <see langword="true" /> for a present element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementPresent_ShouldReturnTrue()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.IsTrue(queue.Contains("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.GetPriority" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetPriority_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.GetPriority(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.GetPriority" /> on a missing element throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void GetPriority_WhenElementMissing_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = queue.GetPriority("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.GetPriority" /> returns the stored priority.
    /// </summary>
    [TestMethod]
    public void GetPriority_WhenElementPresent_ShouldReturnPriority()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 42);

        Assert.AreEqual(42, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryGetPriority" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGetPriority_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.TryGetPriority(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryGetPriority" /> returns <see langword="false" /> for a missing element.
    /// </summary>
    [TestMethod]
    public void TryGetPriority_WhenElementMissing_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsFalse(queue.TryGetPriority("missing", out var priority));
        Assert.AreEqual(0, priority);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryGetPriority" /> returns <see langword="true" /> and the priority for a present element.
    /// </summary>
    [TestMethod]
    public void TryGetPriority_WhenElementPresent_ShouldReturnTrue()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 42);

        Assert.IsTrue(queue.TryGetPriority("a", out var priority));
        Assert.AreEqual(42, priority);
    }

}
