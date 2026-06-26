// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.SignData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains Ed25519-specific tests for <see cref="Ed25519.SignData(ReadOnlySpan{byte})" /> — determinism and the
/// span overload; the round-trip, tamper, and missing-key contracts are inherited from the signature base.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that no public Ed25519 method exposes a context parameter, so callers cannot assume the unimplemented
    /// Ed25519ctx behavior.
    /// </summary>
    [TestMethod]
    public void PublicMethods_WhenInspected_ShouldNotExposeContextParameter()
    {
        IEnumerable<ParameterInfo> parameters = typeof(Ed25519)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetParameters());

        Assert.IsFalse(parameters.Any(parameter => parameter.Name!.Contains("context", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Verifies that no public prehash signing or verification method is exposed, so callers cannot assume the
    /// unimplemented Ed25519ph behavior.
    /// </summary>
    [TestMethod]
    public void PublicMethods_WhenInspected_ShouldNotExposePrehashVariants()
    {
        bool exposesPrehash = typeof(Ed25519)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(method => method.Name.Contains("prehash", StringComparison.OrdinalIgnoreCase));

        Assert.IsFalse(exposesPrehash);
    }

    /// <summary>
    /// Verifies that signing is deterministic per RFC 8032: the same message under the same key yields the
    /// identical signature.
    /// </summary>
    [TestMethod]
    public void SignData_WhenSigningSameMessageTwice_ShouldProduceIdenticalSignatures()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 1, 2, 3, 4, 5 };

        CollectionAssert.AreEqual(algorithm.SignData(message), algorithm.SignData(message));
    }

    /// <summary>
    /// Verifies that the span-writing overload produces the same signature as the allocating overload and rejects
    /// destinations of any other length.
    /// </summary>
    [TestMethod]
    public void SignData_WhenUsingSpanOverload_ShouldMatchAllocatingOverloadAndRejectWrongDestinationLengths()
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
        byte[] message = new byte[] { 0x72 };

        byte[] allocating = algorithm.SignData(message);
        byte[] spanResult = new byte[Ed25519.SignatureSizeInBytes];
        algorithm.SignData(message, spanResult);
        CollectionAssert.AreEqual(allocating, spanResult);

        Assert.ThrowsExactly<ArgumentException>(() => { algorithm.SignData(message, new byte[Ed25519.SignatureSizeInBytes - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { algorithm.SignData(message, new byte[Ed25519.SignatureSizeInBytes + 1]); });
    }
}
