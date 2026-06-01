// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CcmModeTransformTests
    : AeadBlockCipherModeTests<CcmModeTransformTests, CcmModeTransform>
{
    protected override CcmModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, iv);
}
