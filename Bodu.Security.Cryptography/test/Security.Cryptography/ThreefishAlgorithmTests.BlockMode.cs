// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishAlgorithmTests.BlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class ThreefishAlgorithmTests<TTest, TAlgorithm>
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
        using TAlgorithm algorithm = CreateAlgorithm();

        try
        {
            algorithm.BlockMode = mode;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting {typeof(TAlgorithm).Name}.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
