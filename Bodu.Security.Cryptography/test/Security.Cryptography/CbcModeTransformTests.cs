// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CbcModeTransformTests
    : BlockCipherModeTests<CbcModeTransform>
{
    protected override CbcModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new CbcModeTransform(cipher, iv);
}
