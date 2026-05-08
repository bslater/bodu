// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformDisposeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the <see cref="BlockCipherTransform.Dispose" /> contract: deferred input is cleared, the
/// underlying mode transform is disposed (cascade), and a second Dispose call is a no-op. The mode-side
/// state-clearing assertions live with the per-mode tests (e.g. <c>CbcModeTransformTests.Dispose.cs</c>);
/// this class focuses on the cascade itself.
/// </summary>
[TestClass]
public sealed class BlockCipherTransformDisposeTests
{
    private const int BlockSize = 8;

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.Dispose" /> cascades disposal to the underlying
    /// <see cref="IBlockCipherModeTransform" />, observable as the CBC mode's running chaining vector
    /// being zeroed after the cascade.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldCascadeDisposeToMode()
    {
        var cipher = new MonitoringBlockCipher(BlockSize, xorMask: 0xAA);
        var iv = new byte[BlockSize];
        for (int i = 0; i < iv.Length; i++) iv[i] = (byte)(i + 1);

        var transform = new TestBlockCipherTransform(
            cipher, CipherBlockMode.CBC, PaddingMode.None, iv, encrypt: true);

        FieldInfo modeField = typeof(BlockCipherTransform).GetField(
            "_mode", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cbc = (CbcModeTransform)modeField.GetValue(transform)!;

        FieldInfo currentIvField = typeof(CbcModeTransform).GetField(
            "_currentIv", BindingFlags.Instance | BindingFlags.NonPublic)!;
        byte[] currentIvBefore = (byte[])currentIvField.GetValue(cbc)!;
        Assert.IsTrue(currentIvBefore.Any(b => b != 0),
            "Pre-condition: the running CBC chaining vector should be non-zero before disposal.");

        transform.Dispose();

        byte[] currentIvAfter = (byte[])currentIvField.GetValue(cbc)!;
        CollectionAssert.AreEqual(new byte[BlockSize], currentIvAfter,
            "BlockCipherTransform.Dispose must cascade Dispose to the underlying mode, " +
            "observable as the CBC chaining vector being zeroed.");
    }

    /// <summary>
    /// Verifies that calling <see cref="BlockCipherTransform.Dispose" /> more than once is safe.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var transform = new TestBlockCipherTransform(
            new MonitoringBlockCipher(BlockSize, xorMask: 0xAA),
            CipherBlockMode.CBC,
            PaddingMode.None,
            new byte[BlockSize],
            encrypt: true);

        transform.Dispose();
        transform.Dispose();
    }

    /// <summary>
    /// Test-only concrete subclass to instantiate the abstract <see cref="BlockCipherTransform" /> from
    /// outside the algorithm-specific transform types (e.g. <c>BlowfishTransform</c>).
    /// </summary>
    private sealed class TestBlockCipherTransform : BlockCipherTransform
    {
        public TestBlockCipherTransform(
            IBlockCipher cipher,
            CipherBlockMode cipherMode,
            PaddingMode paddingMode,
            byte[] iv,
            bool encrypt)
            : base(cipher, cipherMode, paddingMode, iv, encrypt)
        {
        }
    }
}
