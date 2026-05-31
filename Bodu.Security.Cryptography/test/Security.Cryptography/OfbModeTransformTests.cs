// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class OfbModeTransformTests
    : BlockCipherModeTests<OfbModeTransform>
{
    /// <inheritdoc />
    protected override OfbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, iv);
}
