// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.BlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that every <see cref="CipherBlockMode" /> value can be applied via the <see cref="SetBlockMode" />
    /// hook without throwing — the underlying property is a plain auto-property and must not gain accidental enum
    /// validation.
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
            SetBlockMode(algorithm, mode);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting {typeof(TAlgorithm).Name}.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
