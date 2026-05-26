// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for an <see cref="ICryptoTransform" /> implementation.
/// Concrete subclasses override <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> to plug
/// their transform into the inherited lifecycle and disposal tests.
/// </summary>
/// <typeparam name="TTransform">The transform type under test.</typeparam>
public abstract class CryptoTransformContractTests<TTransform>
    where TTransform : ICryptoTransform
{
    /// <summary>
    /// Creates a fresh encryptor for use in a single test invocation.
    /// </summary>
    /// <returns>A new encryptor.</returns>
    protected abstract TTransform CreateEncryptor();

    /// <summary>
    /// Creates a fresh decryptor for use in a single test invocation.
    /// </summary>
    /// <returns>A new decryptor.</returns>
    protected abstract TTransform CreateDecryptor();

    /// <summary>
    /// Verifies that the encryptor reports a positive <see cref="ICryptoTransform.InputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void Encryptor_InputBlockSize_ShouldBePositive()
    {
        using TTransform encryptor = CreateEncryptor();

        Assert.IsGreaterThan(0, encryptor.InputBlockSize);
    }

    /// <summary>
    /// Verifies that the encryptor reports a positive <see cref="ICryptoTransform.OutputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void Encryptor_OutputBlockSize_ShouldBePositive()
    {
        using TTransform encryptor = CreateEncryptor();

        Assert.IsGreaterThan(0, encryptor.OutputBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> with a zero-length input returns
    /// either an empty buffer or the algorithm's final-pad output depending on the configured padding —
    /// either way, the call must not throw.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenZeroLengthInput_ShouldNotThrow()
    {
        using TTransform encryptor = CreateEncryptor();

        _ = encryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
    }

    /// <summary>
    /// Verifies that disposing the transform twice does not throw, per the standard <see cref="IDisposable" />
    /// idempotence contract.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        TTransform encryptor = CreateEncryptor();

        encryptor.Dispose();
        encryptor.Dispose();
    }
}
