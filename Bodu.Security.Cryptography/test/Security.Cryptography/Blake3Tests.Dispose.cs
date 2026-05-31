// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="Blake3.Dispose(bool)" /> clears every per-subtree chaining value held in the
/// <c>_cvStack</c> rather than leaving the secret-derived state in heap memory.
/// </summary>
public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that disposing a <see cref="Blake3" /> instance which accumulated chaining values across more
    /// than one chunk overwrites every live word of the <c>_cvStack</c> backing buffer and resets the depth
    /// counter to zero. Without this guarantee, the per-chunk chaining values — which are derived from the
    /// message and (for keyed hashing) from the key — survive in heap memory until the GC collects them.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCvStackHasAccumulatedChainingValues_ShouldZeroEachStoredArray()
    {
        var hasher = new Blake3();

        // 2049 bytes spans three 1024-byte chunks, guaranteeing at least one parent CV on the stack.
        var input = new byte[2049];
        for (var i = 0; i < input.Length; i++) input[i] = (byte)(i & 0xFF);

        hasher.TransformBlock(input, 0, input.Length, null, 0);

        FieldInfo? cvStackField = typeof(Blake3).GetField("_cvStack", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? cvStackDepthField = typeof(Blake3).GetField("_cvStackDepth", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(cvStackField, "Blake3._cvStack field must exist for this contract test.");
        Assert.IsNotNull(cvStackDepthField, "Blake3._cvStackDepth field must exist for this contract test.");

        // _cvStack is a readonly flat uint[] buffer; capturing the reference now lets us inspect its contents
        // after Dispose because the reference itself is stable across disposal.
        var stackBuffer = (uint[])cvStackField.GetValue(hasher)!;
        var depthBeforeDispose = (int)cvStackDepthField.GetValue(hasher)!;
        Assert.IsTrue(depthBeforeDispose > 0, "Precondition: hashing 2049 bytes should push at least one chunk CV.");
        Assert.IsTrue(Array.Exists(stackBuffer, v => v != 0),
            "Precondition: stack buffer should hold non-zero chaining values before Dispose.");

        hasher.Dispose();

        Assert.IsTrue(Array.TrueForAll(stackBuffer, v => v == 0),
            "Dispose must zero every word of the _cvStack backing buffer so secret-derived state does not survive in heap memory.");
        Assert.AreEqual(0, (int)cvStackDepthField.GetValue(hasher)!,
            "Dispose must reset _cvStackDepth so a reused instance starts from an empty stack.");
    }
}
