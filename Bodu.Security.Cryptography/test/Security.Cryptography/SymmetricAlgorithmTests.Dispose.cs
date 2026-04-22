// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
{
    /// <summary>
    /// Verifies that attempting to create a cryptographic transform on a disposed
    /// <typeparamref name="TAlgorithm" /> instance throws <see cref="ObjectDisposedException" /> whose
    /// <see cref="ObjectDisposedException.ObjectName" /> carries the concrete algorithm type
    /// name. Regression guard for defects where <c>nameof(T)</c> on a non-generic base class
    /// produced the literal string <c>"T"</c> instead of the derived type name.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCreateEncryptorCalledAfterDispose_ShouldReportConcreteTypeName()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            using var _ = algorithm.CreateEncryptor();
            Assert.Fail("Expected ObjectDisposedException after disposal.");
        }
        catch (ObjectDisposedException ex)
        {
            Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
        }
    }

    /// <summary>
    /// Verifies that an <see cref="ICryptoTransform" /> obtained from the algorithm becomes
    /// unusable once it is disposed. Regression guard for transform disposal paths that
    /// previously leaked their inner cipher or left <c>deferredInput</c> un-cleared.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenTransformIsDisposedAndReused_ShouldNotBeReusable()
    {
        using var algorithm = CreateAlgorithm();
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
        using var algorithm = CreateAlgorithm();
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

}
