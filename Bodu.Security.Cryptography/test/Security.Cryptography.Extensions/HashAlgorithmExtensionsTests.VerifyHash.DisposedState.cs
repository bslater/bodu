// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.VerifyHash.DisposedState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Pins down the <see cref="HashAlgorithmExtensions.VerifyHash(HashAlgorithm, byte[], byte[])" />
/// contract when invoked against a disposed <see cref="HashAlgorithm" />. Each overload routes
/// through <c>ComputeHash</c> or <c>TryComputeHash</c>, which raise
/// <see cref="ObjectDisposedException" /> on the underlying disposed instance — the extension
/// must propagate that exception unchanged rather than silently returning <see langword="false" />,
/// rethrowing as a different type, or yielding an arbitrary success/failure verdict.
/// </summary>
public partial class HashAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that the byte-array overload of <see cref="HashAlgorithmExtensions.VerifyHash" />
    /// propagates <see cref="ObjectDisposedException" /> when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_ByteArray_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(SampleData, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that the hex-string overload propagates <see cref="ObjectDisposedException" />
    /// when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_ByteArrayHex_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(SampleData, SampleHex);
        });
    }

    /// <summary>
    /// Verifies that the span overload propagates <see cref="ObjectDisposedException" /> when the
    /// algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_Span_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash((System.ReadOnlySpan<byte>)SampleData, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that the read-only memory overload propagates <see cref="ObjectDisposedException" />
    /// when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_ReadOnlyMemory_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(new System.ReadOnlyMemory<byte>(SampleData), SampleHash);
        });
    }

    /// <summary>
    /// Verifies that the stream + byte-array overload propagates <see cref="ObjectDisposedException" />
    /// when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_Stream_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        using var stream = new MemoryStream(SampleData);

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(stream, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that the stream + hex-string overload propagates <see cref="ObjectDisposedException" />
    /// when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_StreamHex_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        using var stream = new MemoryStream(SampleData);

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(stream, SampleHex);
        });
    }

    /// <summary>
    /// Verifies that the string + encoding + byte-array overload propagates
    /// <see cref="ObjectDisposedException" /> when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void VerifyHash_StringEncoded_WhenAlgorithmIsDisposed_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.VerifyHash(SampleString, SampleEncoding, SampleStringHash);
        });
    }

    /// <summary>
    /// Verifies that the malformed-hex shortcut path is reached BEFORE
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> would observe the disposed state — the
    /// xml docs document that "a malformed expectedHex string is treated as a non-match and
    /// returns false", so a malformed hex argument should win over the disposed state and the
    /// method should return <see langword="false" /> rather than throw.
    /// </summary>
    [TestMethod]
    public void VerifyHash_ByteArrayHex_WhenAlgorithmIsDisposedAndHexIsMalformed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var result = algorithm.VerifyHash(SampleData, expectedHex: "not-hex");

        Assert.IsFalse(result,
            "A malformed expectedHex argument should short-circuit the verification before the disposed-state check; the documented contract returns false rather than throwing.");
    }

    /// <summary>
    /// Verifies that the stream + malformed-hex overload similarly short-circuits and returns
    /// <see langword="false" /> rather than touching the disposed algorithm.
    /// </summary>
    [TestMethod]
    public void VerifyHash_StreamHex_WhenAlgorithmIsDisposedAndHexIsMalformed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        using var stream = new MemoryStream(SampleData);

        var result = algorithm.VerifyHash(stream, expectedHex: "zz");

        Assert.IsFalse(result,
            "A malformed expectedHex must short-circuit before any work is delegated to the disposed algorithm.");
    }
}
