// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pkcs7PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class Pkcs7PaddingTests
    : PaddingStrategyTests<Pkcs7Padding>
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
