// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.BlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that the algorithm-specific <see cref="Threefish.BlockMode" /> property accepts
    /// every <see cref="CipherBlockMode" /> value without throwing — it is a plain auto-property
    /// and must not gain accidental enum validation.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.ECB)]
    [DataRow(CipherBlockMode.CBC)]
    [DataRow(CipherBlockMode.CFB)]
    [DataRow(CipherBlockMode.OFB)]
    [DataRow(CipherBlockMode.CTR)]
    public void BlockMode_WhenSetToValidValue_ShouldNotThrow(CipherBlockMode mode)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        try
        {
            algorithm.BlockMode = mode;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting Threefish256.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
