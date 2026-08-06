// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that a YAML sequence binds to each collection shape the converter materializes specially, rather than
/// only to the list-like shapes it can populate through <c>Add</c>.
/// </summary>
/// <remarks>
/// Queue, Stack and the three concurrent collections each need their own materializer because none of them exposes
/// an <c>Add</c> method, so each is a distinct path through the collection converter.
/// </remarks>
public partial class YamlSerializerTests
{
    /// <summary>The sequence every collection case in this file binds.</summary>
    private const string Sequence = "- 1\n- 2\n- 3\n";

    /// <summary>
    /// Verifies that a sequence binds to a <see cref="Queue{T}" /> preserving document order, so the first element
    /// of the document is the head of the queue.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsQueue_ShouldPreserveDocumentOrder()
    {
        Queue<int> queue = YamlSerializer.Deserialize<Queue<int>>(Sequence)!;

        Assert.AreEqual(3, queue.Count);
        Assert.AreEqual(1, queue.Peek());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());
    }

    /// <summary>
    /// Verifies that a sequence binds to a <see cref="Stack{T}" />, pushing in document order so the last element of
    /// the document ends up on top.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsStack_ShouldPushInDocumentOrder()
    {
        Stack<int> stack = YamlSerializer.Deserialize<Stack<int>>(Sequence)!;

        Assert.AreEqual(3, stack.Count);
        Assert.AreEqual(3, stack.Peek());
    }

    /// <summary>
    /// Verifies that a sequence binds to a <see cref="ConcurrentQueue{T}" /> preserving document order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsConcurrentQueue_ShouldPreserveDocumentOrder()
    {
        ConcurrentQueue<int> queue = YamlSerializer.Deserialize<ConcurrentQueue<int>>(Sequence)!;

        Assert.AreEqual(3, queue.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());
    }

    /// <summary>
    /// Verifies that a sequence binds to a <see cref="ConcurrentStack{T}" />, with the last element of the document
    /// on top.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsConcurrentStack_ShouldPushInDocumentOrder()
    {
        ConcurrentStack<int> stack = YamlSerializer.Deserialize<ConcurrentStack<int>>(Sequence)!;

        Assert.AreEqual(3, stack.Count);
        Assert.IsTrue(stack.TryPeek(out int top));
        Assert.AreEqual(3, top);
    }

    /// <summary>
    /// Verifies that a sequence binds to a <see cref="ConcurrentBag{T}" />. A bag is unordered, so only membership
    /// is asserted.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsConcurrentBag_ShouldContainEveryElement()
    {
        ConcurrentBag<int> bag = YamlSerializer.Deserialize<ConcurrentBag<int>>(Sequence)!;

        Assert.AreEqual(3, bag.Count);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, bag.ToArray());
    }

    /// <summary>
    /// Verifies that an empty sequence binds to each specially-materialized shape without faulting.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSequenceIsEmpty_ShouldBindEmptyCollections()
    {
        const string empty = "[]";

        Assert.AreEqual(0, YamlSerializer.Deserialize<Queue<int>>(empty)!.Count);
        Assert.AreEqual(0, YamlSerializer.Deserialize<Stack<int>>(empty)!.Count);
        Assert.AreEqual(0, YamlSerializer.Deserialize<ConcurrentQueue<int>>(empty)!.Count);
        Assert.AreEqual(0, YamlSerializer.Deserialize<ConcurrentStack<int>>(empty)!.Count);
        Assert.AreEqual(0, YamlSerializer.Deserialize<ConcurrentBag<int>>(empty)!.Count);
    }
}
