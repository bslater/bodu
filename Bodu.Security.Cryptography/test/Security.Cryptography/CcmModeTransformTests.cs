// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CcmModeTransformTests
    : AeadBlockCipherModeTests<CcmModeTransformTests, CcmModeTransform>
{
    protected override CcmModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new CcmModeTransform(cipher, iv);
}
