// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeTests.EmptyInput.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Pins the empty-input no-op contract for every <see cref="IBlockCipherModeTransform" />
/// implementation. <see cref="CbcModeTransform" />, <see cref="CfbModeTransform" />,
/// <see cref="OfbModeTransform" />, <see cref="EcbModeTransform" />, and
/// <see cref="XtsModeTransform" /> all call
/// <c>CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false)</c>
/// (the consolidation in #200), so a zero-length input must be a no-op that returns zero bytes
/// written. <see cref="CtsModeTransform" /> requires at least one full block by design and opts
/// out via <see cref="AcceptsEmptyInput" />.
/// </summary>
/// Pins the empty-input no-op contract for every <see cref="IBlockCipherModeTransform" />
/// implementation. <see cref="CbcModeTransform" />, <see cref="CfbModeTransform" />,
/// <see cref="OfbModeTransform" />, <see cref="EcbModeTransform" />, and
/// <see cref="XtsModeTransform" /> all call
/// <c>CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false)</c>
/// (the consolidation in #200), so a zero-length input must be a no-op that returns zero bytes
/// written. <see cref="CtsModeTransform" /> requires at least one full block by design and opts
/// out via <see cref="AcceptsEmptyInput" />.
/// </summary>
/// <remarks>
/// Pre-#200 every mode except CBC used the default <c>throwIfZero: true</c>, so a zero-length
/// input surfaced <see cref="System.Security.Cryptography.CryptographicException" /> with a
/// "block length must be a positive multiple" message — incompatible with the
/// <see cref="System.Security.Cryptography.CryptoStream.FlushFinalBlock" /> path which always
/// invokes the transform at end-of-stream regardless of whether data was written. This test
/// keeps the post-#200 behaviour locked in.
/// </remarks>
public abstract partial class BlockCipherModeTests<TMode>
{
    /// <summary>
    /// Gets a value indicating whether this mode transform accepts a zero-length input as a no-op.
    /// CBC and CTR document this contract; CFB / OFB / ECB / XTS currently reject it but should
    /// match the CBC behaviour for consistency. <see cref="CtsModeTransform" /> requires at least
    /// one full block by design and therefore overrides this to <see langword="false" />.
    /// </summary>
    protected virtual bool AcceptsEmptyInput => true;

    /// <summary>
    /// Verifies that <see cref="IBlockCipherModeTransform.Transform" /> with a zero-length input
    /// span is a no-op rather than throwing — the consistency contract that
    /// <see cref="CbcModeTransform" /> already honours via <c>throwIfZero: false</c>. The other
    /// confidentiality-only modes currently throw <see cref="System.Security.Cryptography.CryptographicException" />
    /// here; this test fails until they are aligned with CBC.
    /// span is a no-op rather than throwing — the consistency contract every mode honours via
    /// <c>throwIfZero: false</c> as of #200.
    /// </summary>
    [TestMethod]
    public void Transform_WhenInputIsEmpty_ShouldBeNoOp()
    {
        if (!AcceptsEmptyInput)
        {
            Assert.Inconclusive(
                $"{typeof(TMode).Name} explicitly rejects sub-block input by design; the empty-input no-op contract does not apply.");
            return;
        }

        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        var iv = UsesInitializationVector
            ? CryptographyHelper.GetRandomNonZeroBytes(ExpectedBlockSize)
            : new byte[ExpectedBlockSize];
        TMode transform = CreateTransform(cipher, iv);

        var written = 0;
        try
        {
            written = transform.Transform([], [], encrypt: true);
        }
        catch (System.Exception ex)
        {
            Assert.Fail(
                $"{typeof(TMode).Name}.Transform with an empty input span should be a no-op consistent with CbcModeTransform, " +
                $"but it threw {ex.GetType().Name}: {ex.Message}");
        }

        Assert.AreEqual(0, written,
            $"{typeof(TMode).Name}.Transform with an empty input span should report zero bytes written.");
    }
}
