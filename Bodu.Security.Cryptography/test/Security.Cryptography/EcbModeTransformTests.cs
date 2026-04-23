// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class EcbModeTransformTests
    : BlockCipherModeTests<EcbModeTransform>
{
    /// <inheritdoc />
    protected override EcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new EcbModeTransform(cipher);

    /// <inheritdoc />
    /// <remarks>ECB processes every block independently and takes no initialisation vector.</remarks>
    protected override bool UsesInitializationVector => false;

    /// <inheritdoc />
    /// <remarks>ECB has no chaining; identical plaintext blocks always yield identical ciphertext blocks.</remarks>
    protected override bool UsesChaining => false;
}
