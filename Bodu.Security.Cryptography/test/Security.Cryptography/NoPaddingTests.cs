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
    /// <see cref="NoPadding.Pad" /> rejects input whose length is not already a multiple of the block size, so the
    /// round-trip test only exercises residual 0.
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
