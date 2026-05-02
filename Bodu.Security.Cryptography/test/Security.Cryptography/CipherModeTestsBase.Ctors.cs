// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherModeTestsBase.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class CipherModeTestsBase<TTransform>
{
    /// <summary>
    /// Gets the expected initialisation vector, nonce, or initial counter value size, in bytes.
    /// </summary>
    /// <remarks>
    /// Most block cipher modes require an initialisation vector whose size matches the cipher block size.
    /// AEAD modes such as GCM may override this value because their public constructor accepts a nonce rather
    /// than a block-sized IV.
    /// </remarks>
    protected virtual int ExpectedInitializationVectorSize => ExpectedBlockSize;

    // ── Constructor validation tests ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that constructing the mode transform with a <see langword="null" /> initialisation value throws
    /// <see cref="ArgumentNullException" /> whose <see cref="ArgumentException.ParamName" /> equals
    /// <see cref="IvParameterName" />. Skipped for modes that do not accept an initialisation value, such as ECB.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIvIsNull_ShouldThrowArgumentNullExceptionWithIvParamName()
    {
        if (!UsesInitializationVector)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} does not accept an initialisation vector.");
            return;
        }

        var cipher = new MonitoringBlockCipher(ExpectedBlockSize);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CreateTransform(cipher, null!);
        });

        Assert.AreEqual(IvParameterName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing the mode transform with an initialisation value whose length is invalid throws
    /// <see cref="ArgumentException" /> whose <see cref="ArgumentException.ParamName" /> equals
    /// <see cref="IvParameterName" />. Skipped for modes that do not accept an initialisation value, such as ECB.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIvLengthDoesNotMatchExpectedSize_ShouldThrowArgumentException()
    {
        if (!UsesInitializationVector)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} does not accept an initialisation vector.");
            return;
        }

        var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
        var iv = new byte[Math.Max(0, ExpectedInitializationVectorSize - 1)];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = CreateTransform(cipher, iv);
        });

        Assert.AreEqual(IvParameterName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing the mode transform with a valid cipher and a correctly-sized initialisation value
    /// completes without throwing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArgumentsAreValid_ShouldSucceed()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
        var iv = UsesInitializationVector
            ? new byte[ExpectedInitializationVectorSize]
            : Array.Empty<byte>();

        var transform = CreateTransform(cipher, iv);

        Assert.IsNotNull(transform);
    }
}