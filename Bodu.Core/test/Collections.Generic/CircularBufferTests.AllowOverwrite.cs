// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.AllowOverwrite.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that AllowOverwrite reflects the value set in the constructor and can be
    /// updated at runtime.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggled_ShouldReflectUpdatedValue()
    {
        var buffer = new CircularBuffer<int>(3, false);
        Assert.IsFalse(buffer.AllowOverwrite);

        buffer.AllowOverwrite = true;
        Assert.IsTrue(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that toggling AllowOverwrite does not affect the current contents of the buffer.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggled_ShouldNotAffectContents()
    {
        var buffer = new CircularBuffer<string>(3, allowOverwrite: false);
        buffer.Enqueue("X");
        buffer.Enqueue("Y");

        buffer.AllowOverwrite = true;
        buffer.Enqueue("Z");

        CollectionAssert.AreEqual(new[] { "X", "Y", "Z" }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.AllowOverwrite" /> controls overwriting behaviour when toggled at runtime.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenDisabledAndBufferFull_ShouldPreventOverwrite()
    {
        var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        // Disable overwrite
        buffer.AllowOverwrite = false;

        Assert.ThrowsExactly<InvalidOperationException>(() => buffer.Enqueue(4));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that enabling AllowOverwrite at runtime permits overwriting existing items.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenReEnabled_ShouldPermitOverwrite()
    {
        var buffer = new CircularBuffer<int>(3, allowOverwrite: false);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        // Enable overwrite at runtime
        buffer.AllowOverwrite = true;
        buffer.Enqueue(4); // should evict 1

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, buffer.ToArray());
    }
}
