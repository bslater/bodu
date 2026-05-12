// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.DebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that the <see cref="DebuggerTypeProxyAttribute"/> attached to <see cref="ConcurrentCircularBuffer{T}"/>
    /// references a proxy whose constructor accepts a <see cref="ConcurrentCircularBuffer{T}"/> and that the proxy
    /// can be instantiated without throwing.
    /// </summary>
    /// <remarks>
    /// Guards against the latent defect where the attribute previously pointed to <c>CircularBufferDebugView&lt;&gt;</c>,
    /// whose constructor accepts only the non-concurrent <c>CircularBuffer&lt;T&gt;</c>. Inspection in a debugger would
    /// have thrown <see cref="ArgumentException"/> at construction time.
    /// </remarks>
    [TestMethod]
    public void DebugView_WhenAttachedAttributeResolved_ShouldAcceptConcurrentBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        DebuggerTypeProxyAttribute? proxy = typeof(ConcurrentCircularBuffer<TestItem>)
            .GetCustomAttributes<DebuggerTypeProxyAttribute>(inherit: false)
            .SingleOrDefault();

        Assert.IsNotNull(proxy, "ConcurrentCircularBuffer<T> is missing its DebuggerTypeProxy attribute.");

        Type proxyType = Type.GetType(proxy!.ProxyTypeName!, throwOnError: true)!
            .MakeGenericType(typeof(TestItem));

        var instance = Activator.CreateInstance(proxyType, buffer)!;
        Assert.IsNotNull(instance);
    }

    /// <summary>
    /// Verifies that the debug view's <c>Items</c> property returns a snapshot whose contents match the buffer.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenItemsRead_ShouldReturnFifoSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity: 4);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        var view = new ConcurrentCircularBufferDebugView<TestItem>(buffer);
        TestItem[] items = view.Items;

        Assert.AreEqual(3, items.Length);
        Assert.AreEqual(10, items[0].Value);
        Assert.AreEqual(20, items[1].Value);
        Assert.AreEqual(30, items[2].Value);
    }

    /// <summary>
    /// Verifies that constructing the debug view with a <see langword="null"/> buffer throws
    /// <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenBufferIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentCircularBufferDebugView<TestItem>(null!);
        });
    }
}
