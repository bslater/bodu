// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.Defaults.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> reports the
    /// <see cref="SymmetricAlgorithmSpecification.BlockSizeBits" /> declared by the specification.
    /// </summary>
    [TestMethod]
    public void BlockSize_WhenDefault_ShouldMatchSpec()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification specification = GetSpecification();
        Assert.AreEqual(specification.BlockSizeBits, algorithm.BlockSize);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> exposes the
    /// <see cref="SymmetricAlgorithmSpecification.DefaultPadding" /> declared by the specification.
    /// </summary>
    [TestMethod]
    public void Padding_WhenDefault_ShouldMatchSpec()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification specification = GetSpecification();
        Assert.AreEqual(specification.DefaultPadding, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> exposes the
    /// <see cref="SymmetricAlgorithmSpecification.DefaultMode" /> declared by the specification.
    /// </summary>
    [TestMethod]
    public void Mode_WhenDefault_ShouldMatchSpec()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification specification = GetSpecification();
        Assert.AreEqual(specification.DefaultMode, algorithm.Mode);
    }
}
