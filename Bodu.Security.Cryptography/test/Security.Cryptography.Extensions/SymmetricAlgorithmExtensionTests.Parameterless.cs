// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.Parameterless.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public partial class SymmetricAlgorithmExtensionTests
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithmExtensions.TryCreateEncryptor(SymmetricAlgorithm, out ICryptoTransform)" />
    /// returns <see langword="false" /> and a null transform when the algorithm has been disposed, exercising
    /// the catch path of the parameterless overload.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_Parameterless_WhenAlgorithmIsDisposed_ShouldReturnFalseAndNullOutput()
    {
        SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        bool result = algorithm.TryCreateEncryptor(out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithmExtensions.TryCreateDecryptor(SymmetricAlgorithm, out ICryptoTransform)" />
    /// returns <see langword="false" /> and a null transform when the algorithm has been disposed, exercising
    /// the catch path of the parameterless overload.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_Parameterless_WhenAlgorithmIsDisposed_ShouldReturnFalseAndNullOutput()
    {
        SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        bool result = algorithm.TryCreateDecryptor(out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }
}
