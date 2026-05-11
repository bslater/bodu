// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NoPaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class NoPaddingTests
    : PaddingStrategyTests<NoPadding>
{
    protected override int BlockSize => 16;

    protected override bool ValidatesPaddingOnUnpad => false;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="NoPadding.Unpad" /> ignores its <c>blockSize</c> parameter — it returns the input unchanged
    /// regardless of alignment. The block-size validation tests are therefore inapplicable.
    /// </remarks>
    protected override bool ValidatesBlockSizeOnUnpad => false;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="NoPadding.Pad" /> rejects input whose length is not already a multiple of the block size, so the
    /// round-trip test only exercises residual 0.
    /// </remarks>
    protected override bool SupportsUnalignedInput => false;

    protected override byte[] CreatePlaintextWithResidual(int residualBytes)
    {
        var buf = new byte[residualBytes];
        for (var i = 0; i < buf.Length; i++)
            buf[i] = (byte)(0x30 + i);
        return buf;
    }
}
