// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class OfbModeTransformTests
    : BlockCipherModeTests<OfbModeTransform>
{
    /// <inheritdoc />
    protected override OfbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new OfbModeTransform(cipher, iv);
}
