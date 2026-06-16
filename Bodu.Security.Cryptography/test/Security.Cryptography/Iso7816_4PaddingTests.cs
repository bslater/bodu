// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7816_4PaddingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class Iso7816_4PaddingTests
    : PaddingStrategyTests<Iso7816_4Padding>
{
    protected override int BlockSize => 16;

    protected override bool ValidatesPaddingOnUnpad => true;

    /// <inheritdoc />
    /// <remarks>
    /// ISO/IEC 7816-4 uses a terminator pattern (<c>0x80</c> followed by <c>0x00</c>)
    /// rather than a trailing length byte, so tests built around a length-byte layout
    /// do not apply to this scheme.
    /// </remarks>
    protected override bool HasLengthByte => false;

    protected override byte[] CreatePlaintextWithResidual(int residualBytes)
    {
        byte[] buf = new byte[residualBytes];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (byte)(0x30 + i);
        return buf;
    }
}
