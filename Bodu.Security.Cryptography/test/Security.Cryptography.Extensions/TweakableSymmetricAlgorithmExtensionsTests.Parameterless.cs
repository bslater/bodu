// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensionsTests.Parameterless.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public partial class TweakableSymmetricAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithmExtensions.TryCreateEncryptor(TweakableSymmetricAlgorithm, out ICryptoTransform)" />
    /// returns <see langword="false" /> and a null transform when the algorithm has been disposed, exercising
    /// the catch path of the parameterless overload.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenAlgorithmIsDisposed_ShouldReturnFalseAndNullOutput()
    {
        TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var result = algorithm.TryCreateEncryptor(out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithmExtensions.TryCreateDecryptor(TweakableSymmetricAlgorithm, out ICryptoTransform)" />
    /// returns <see langword="false" /> and a null transform when the algorithm has been disposed, exercising
    /// the catch path of the parameterless overload.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenAlgorithmIsDisposed_ShouldReturnFalseAndNullOutput()
    {
        TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var result = algorithm.TryCreateDecryptor(out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }
}
