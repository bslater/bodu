// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class EaxModeTransformTests
    : AeadBlockCipherModeTests<EaxModeTransformTests, EaxModeTransform>
{
    /// <inheritdoc />
    protected override EaxModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new EaxModeTransform(cipher, iv);
}
