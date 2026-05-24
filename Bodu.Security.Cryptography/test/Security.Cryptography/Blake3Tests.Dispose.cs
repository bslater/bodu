// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Reflection;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="Blake3.Dispose(bool)" /> clears every per-subtree chaining value held in the
/// <c>_cvStack</c> rather than just dropping the list container's references.
/// </summary>
public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that disposing a <see cref="Blake3" /> instance which accumulated chaining values across more
    /// than one chunk overwrites the contents of every <c>uint[]</c> stored on <c>_cvStack</c> before the list
    /// itself is cleared. Without this guarantee, the per-chunk chaining values — which are derived from the
    /// message and (for keyed hashing) from the key — survive in heap memory until the GC collects them.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCvStackHasAccumulatedChainingValues_ShouldZeroEachStoredArray()
    {
        var hasher = new Blake3();

        // 2049 bytes spans three 1024-byte chunks, guaranteeing at least one parent CV on the stack.
        byte[] input = new byte[2049];
        for (int i = 0; i < input.Length; i++) input[i] = (byte)(i & 0xFF);

        hasher.TransformBlock(input, 0, input.Length, null, 0);

        FieldInfo? cvStackField = typeof(Blake3).GetField("_cvStack", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(cvStackField, "Blake3._cvStack field must exist for this contract test.");

        IList stack = (IList)cvStackField.GetValue(hasher)!;
        Assert.IsTrue(stack.Count > 0, "Precondition: hashing 2049 bytes should push at least one chunk CV.");

        // Capture a strong reference to one of the stored arrays so it survives the list-clear in Dispose,
        // letting the test inspect its contents after disposal.
        uint[] capturedCv = (uint[])stack[0]!;
        Assert.IsTrue(Array.Exists(capturedCv, v => v != 0),
            "Precondition: stored chunk CV should be non-zero before Dispose.");

        hasher.Dispose();

        Assert.IsTrue(Array.TrueForAll(capturedCv, v => v == 0),
            "Dispose must zero the contents of every CV array on _cvStack, not just clear the list container.");
    }
}
