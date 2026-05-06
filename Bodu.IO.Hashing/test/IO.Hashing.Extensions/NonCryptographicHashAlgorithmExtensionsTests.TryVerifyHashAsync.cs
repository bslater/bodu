// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the asynchronous try-pattern <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHashAsync" />
/// overloads.
/// </summary>
/// <remarks>
/// The try-pattern async overloads catch all exceptions (including
/// <see cref="OperationCanceledException" />) and return <see langword="false" /> rather than propagating them, with
/// the sole exception of a <see langword="null" /> algorithm receiver which still throws
/// <see cref="ArgumentNullException" />.
/// </remarks>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Stream + byte-array ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a non-matching stream input returns <see langword="false" /> for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamDoesNotMatchByteArray_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);
        byte[] wrong = BitConverter.GetBytes((uint)999);

        bool result = await algorithm.TryVerifyHashAsync(stream, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> rather than throwing for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForByteArrayOverload_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();

        bool result = await algorithm.TryVerifyHashAsync((Stream)null!, SampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hash returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenExpectedHashIsNull_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        bool result = await algorithm.TryVerifyHashAsync(stream, (byte[])null!);

        Assert.IsFalse(result);
    }

    // ─── Stream + hex string ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the hex overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHex);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> for the hex overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForHexOverload_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();

        bool result = await algorithm.TryVerifyHashAsync((Stream)null!, SampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a malformed hex string returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenHexStringIsMalformed_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        bool result = await algorithm.TryVerifyHashAsync(stream, "ZZZZZZZZ");

        Assert.IsFalse(result);
    }

    // ─── Stream + ReadOnlyMemory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the memory overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);
        ReadOnlyMemory<byte> expected = SampleHash;

        bool result = await algorithm.TryVerifyHashAsync(stream, expected);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> for the memory overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForMemoryOverload_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> expected = SampleHash;

        bool result = await algorithm.TryVerifyHashAsync((Stream)null!, expected);

        Assert.IsFalse(result);
    }

    // ─── Byte-array input ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when expected hash is a byte array.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesByteArray_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();

        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when expected hash is a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();

        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a non-matching byte-array input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayDoesNotMatch_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        byte[] wrong = BitConverter.GetBytes((uint)999);

        bool result = await algorithm.TryVerifyHashAsync(SampleData, wrong);

        Assert.IsFalse(result);
    }

    // ─── String + encoding ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching encoded string input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringEncodedMatches_ShouldReturnTrue()
    {
        var algorithm = CreateAlgorithm();

        bool result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a non-matching encoded string input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringEncodedDoesNotMatch_ShouldReturnFalse()
    {
        var algorithm = CreateAlgorithm();
        byte[] wrong = BitConverter.GetBytes((uint)999);

        bool result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, wrong);

        Assert.IsFalse(result);
    }

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm receiver still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm!.TryVerifyHashAsync(stream, SampleHash));
    }

    /// <summary>
    /// Verifies that a null expected hex string still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenExpectedHexIsNull_ShouldThrowArgumentNullException()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.TryVerifyHashAsync(stream, (string)null!));
    }
}
