// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class CtrModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Dispose" /> zeroes both the running counter and
    /// the retained initial-counter copy so that key-equivalent counter state does not linger in
    /// memory after disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldZeroCounterAndInitialCounter()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var initialCounter = new byte[ExpectedBlockSize];
        for (var i = 0; i < initialCounter.Length; i++) initialCounter[i] = (byte)(i + 1);

        var transform = new CtrModeTransform(cipher, initialCounter);

        // Run a block so the counter has been incremented past its initial state.
        var input = new byte[ExpectedBlockSize];
        var output = new byte[ExpectedBlockSize];
        transform.Transform(input, output, encrypt: true);

        transform.Dispose();

        FieldInfo counterField = typeof(CtrModeTransform).GetField(
            "_counter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo initialField = typeof(CtrModeTransform).GetField(
            "_initialCounter", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var counter = (byte[])counterField.GetValue(transform)!;
        var initial = (byte[])initialField.GetValue(transform)!;

        CollectionAssert.AreEqual(new byte[ExpectedBlockSize], counter,
            "CtrModeTransform.Dispose must zero the running counter.");
        CollectionAssert.AreEqual(new byte[ExpectedBlockSize], initial,
            "CtrModeTransform.Dispose must zero the retained initial counter.");
    }

    /// <summary>
    /// Verifies that calling <see cref="CtrModeTransform.Dispose" /> more than once is safe.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var transform = new CtrModeTransform(cipher, new byte[ExpectedBlockSize]);

        transform.Dispose();
        transform.Dispose();
    }
}
