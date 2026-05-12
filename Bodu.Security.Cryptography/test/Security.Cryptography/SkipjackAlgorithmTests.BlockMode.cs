// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.BlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SkipjackAlgorithmTests
{
    /// <summary>
    /// Verifies that the algorithm-specific <see cref="Skipjack.BlockMode" /> property accepts
    /// every <see cref="CipherModeKind" /> value without throwing — it is a plain auto-property
    /// and must not gain accidental enum validation.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.ECB)]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void BlockMode_WhenSetToValidValue_ShouldNotThrow(CipherModeKind mode)
    {
        using Skipjack algorithm = CreateAlgorithm();

        try
        {
            algorithm.BlockMode = mode;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting Skipjack.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
