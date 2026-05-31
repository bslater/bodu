// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class GcmSivModeTransformTests
    : AeadBlockCipherModeTests<GcmSivModeTransformTests, GcmSivModeTransform>
{
    protected override GcmSivModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, k => new AesBlockCipherFixture(k), iv);
}
