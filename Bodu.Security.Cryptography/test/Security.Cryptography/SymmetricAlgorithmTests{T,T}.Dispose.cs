// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests{T,T}.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that an <see cref="ICryptoTransform" /> obtained from the algorithm becomes
    /// unusable once it is disposed. Regression guard for transform disposal paths that
    /// previously leaked their inner cipher or left <c>deferredInput</c> un-cleared.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenTransformIsDisposedAndReused_ShouldNotBeReusable()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.GenerateIV();
        algorithm.GenerateKey();

        ICryptoTransform encryptor = algorithm.CreateEncryptor();
        byte[] input = new byte[encryptor.InputBlockSize];
        byte[] output = new byte[encryptor.OutputBlockSize];
        encryptor.TransformBlock(input, 0, input.Length, output, 0);

        encryptor.Dispose();

        bool threw = false;
        try
        {
            encryptor.TransformBlock(input, 0, input.Length, output, 0);
        }
        catch (Exception)
        {
            threw = true;
        }

        Assert.IsTrue(threw, $"{typeof(TAlgorithm).Name} transform should not be reusable after Dispose.");
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> twice on a transform obtained
    /// from the algorithm is safe and idempotent.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenTransformIsDisposedTwice_ShouldNotThrow()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.GenerateIV();
        algorithm.GenerateKey();

        ICryptoTransform encryptor = algorithm.CreateEncryptor();
        encryptor.Dispose();

        try
        {
            encryptor.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on {typeof(TAlgorithm).Name} transform threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricAlgorithm.Dispose()" /> twice on the same
    /// instance is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on {typeof(TAlgorithm).Name} threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <typeparamref name="TAlgorithm" /> instance —
    /// one that has never had any property accessed or transform created — completes without
    /// throwing. Regression guard for disposal paths that touch lazily-initialised state without
    /// null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        TAlgorithm algorithm = CreateAlgorithm();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched {typeof(TAlgorithm).Name} instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
