// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZeroPaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class ZeroPaddingTests
    : PaddingStrategyTests<ZeroPadding>
{
    protected override int BlockSize => 16;

    protected override bool ValidatesPaddingOnUnpad => false;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="ZeroPadding.Unpad" /> ignores its <c>blockSize</c> parameter — it returns the input unchanged
    /// because zero-padding bytes cannot be distinguished from legitimate trailing zero bytes. The block-size
    /// validation tests are therefore inapplicable.
    /// </remarks>
    protected override bool ValidatesBlockSizeOnUnpad => false;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="ZeroPadding.Unpad" /> cannot distinguish padding zero bytes from legitimate trailing zero bytes in the
    /// plaintext, so it cannot recover the original length for residuals greater than zero. The round-trip test only
    /// exercises residual 0 (where Pad and Unpad are both no-ops).
    /// </remarks>
    protected override bool SupportsUnalignedInput => false;

    protected override byte[] CreatePlaintextWithResidual(int residualBytes)
    {
        byte[] buf = new byte[residualBytes];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (byte)(0x30 + i);
        return buf;
    }
}
