// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class Iso10126PaddingTests
    : PaddingStrategyTests<Iso10126Padding>
{
    protected override int BlockSize => 16;

    protected override bool ValidatesPaddingOnUnpad => true;

    /// <inheritdoc />
    /// <remarks>
    /// ISO 10126 pad bytes are random and cannot be validated on decryption, so the
    /// generic tamper test for interior bytes does not apply to this scheme.
    /// </remarks>
    protected override bool ValidatesInteriorPaddingOnUnpad => false;

    protected override byte[] CreatePlaintextWithResidual(int residualBytes)
    {
        byte[] buf = new byte[residualBytes];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (byte)(0x30 + i);
        return buf;
    }
}
