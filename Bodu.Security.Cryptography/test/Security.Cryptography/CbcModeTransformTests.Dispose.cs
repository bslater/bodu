// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransformTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class CbcModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Dispose" /> zeroes the running chaining-vector
    /// (<c>_currentIv</c>) so that key-equivalent IV state does not linger in memory after disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldZeroCurrentIv()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        for (var i = 0; i < iv.Length; i++) iv[i] = (byte)(i + 1); // non-zero so zeroing is observable

        var transform = new CbcModeTransform(cipher, iv);

        // Run a block so the IV has evolved past the constructor copy.
        var input = new byte[ExpectedBlockSize];
        var output = new byte[ExpectedBlockSize];
        transform.Transform(input, output, encrypt: true);

        transform.Dispose();

        FieldInfo field = typeof(CbcModeTransform).GetField(
            "_currentIv", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var currentIv = (byte[])field.GetValue(transform)!;

        CollectionAssert.AreEqual(
            new byte[ExpectedBlockSize], currentIv,
            "CbcModeTransform.Dispose must zero the running chaining vector.");
    }

    /// <summary>
    /// Verifies that calling <see cref="CbcModeTransform.Dispose" /> more than once is safe.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var transform = new CbcModeTransform(cipher, new byte[ExpectedBlockSize]);

        transform.Dispose();
        transform.Dispose();
    }
}
