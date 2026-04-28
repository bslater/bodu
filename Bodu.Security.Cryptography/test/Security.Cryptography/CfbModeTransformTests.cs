// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CfbModeTransformTests
    : BlockCipherModeTests<CfbModeTransform>
{
    /// <inheritdoc />
    protected override CfbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new CfbModeTransform(cipher, iv);
}
