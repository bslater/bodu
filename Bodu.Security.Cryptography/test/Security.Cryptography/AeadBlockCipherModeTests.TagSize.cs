// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.TagSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTransform>
{
    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.TagSize" /> is greater than zero.
    /// A zero-length tag provides no authentication and is not a valid AEAD configuration.
    /// </summary>
    [TestMethod]
    public void TagSize_ShouldBePositive()
    {
        var transform = MakeTransform();
        Assert.IsTrue(transform.TagSize > 0, "TagSize must be greater than zero.");
    }
}
