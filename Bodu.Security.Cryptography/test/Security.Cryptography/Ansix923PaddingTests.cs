// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ansix923PaddingTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class Ansix923PaddingTests
    : PaddingStrategyTests<Ansix923Padding>
{
    protected override int BlockSize => 16;

    protected override bool ValidatesPaddingOnUnpad => true;

    protected override byte[] CreatePlaintextWithResidual(int residualBytes)
    {
        var buf = new byte[residualBytes];
        for (var i = 0; i < buf.Length; i++)
            buf[i] = (byte)(0x30 + i);
        return buf;
    }
}
