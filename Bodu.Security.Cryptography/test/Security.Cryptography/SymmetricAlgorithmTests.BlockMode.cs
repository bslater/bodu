// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.BlockMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> reports the
    /// <see cref="SymmetricAlgorithmSpecification.DefaultBlockMode" /> declared by the specification through
    /// the Bodu-specific <c>BlockMode</c> property.
    /// </summary>
    [TestMethod]
    public void BlockMode_WhenDefault_ShouldMatchSpec()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification specification = GetSpecification();
        Assert.AreEqual(specification.DefaultBlockMode, GetBlockMode(algorithm));
    }

    /// <summary>
    /// Verifies that every <see cref="CipherModeKind" /> value can be applied via the <see cref="SetBlockMode" />
    /// hook without throwing — the underlying property is a plain auto-property and must not gain accidental enum
    /// validation.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.ECB)]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void BlockMode_WhenSetToValidValue_ShouldNotThrow(CipherModeKind mode)
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

    /// <summary>
    /// Verifies that setting the Bodu-specific <c>BlockMode</c> property persists and is returned by the next
    /// read via <see cref="GetBlockMode" />.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.ECB)]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void BlockMode_WhenSet_ShouldReturnSameValueOnGet(CipherModeKind mode)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetBlockMode(algorithm, mode);
        Assert.AreEqual(mode, GetBlockMode(algorithm));
    }
}
