// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the <see cref="Scrypt" /> destination-buffer and random-salt overloads, and the PHC-string decode failures
/// surfaced by <see cref="Scrypt.Verify(string, ReadOnlySpan{byte})" />.
/// </summary>
[TestClass]
public sealed class ScryptCoverageTests
{
    private static readonly byte[] Password = System.Text.Encoding.ASCII.GetBytes("password");
    private static readonly byte[] Salt = System.Text.Encoding.ASCII.GetBytes("seasalt!");

    /// <summary>
    /// Verifies that the instance destination-buffer overload fills the supplied span.
    /// </summary>
    [TestMethod]
    public void DeriveKey_IntoDestination_ShouldFillBuffer()
    {
        Span<byte> destination = stackalloc byte[32];

        new Scrypt(2, 1, 1).DeriveKey(Password, Salt, destination);

        Assert.Contains(b => b != 0, destination.ToArray());
    }

    /// <summary>
    /// Verifies that the random-salt hash overload produces a non-empty PHC encoded-hash string.
    /// </summary>
    [TestMethod]
    public void Hash_WithRandomSalt_ShouldProduceEncodedString()
    {
        string encoded = new Scrypt(2, 1, 1).Hash(Password, 32);

        Assert.IsTrue(encoded.StartsWith("$scrypt$", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the static destination-buffer overload fills the supplied span.
    /// </summary>
    [TestMethod]
    public void DeriveKey_StaticIntoDestination_ShouldFillBuffer()
    {
        Span<byte> destination = stackalloc byte[32];

        Scrypt.DeriveKey(Password, Salt, 2, 1, 1, destination);

        Assert.Contains(b => b != 0, destination.ToArray());
    }

    /// <summary>
    /// Verifies that decoding a PHC string whose algorithm field is not <c>scrypt</c> throws.
    /// </summary>
    [TestMethod]
    public void Verify_WhenAlgorithmFieldWrong_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Scrypt.Verify("$notscrypt$ln=14,r=8,p=1$c2FsdA$aGFzaA", Password));
    }

    /// <summary>
    /// Verifies that decoding a PHC string with a parameter lacking <c>=</c> throws.
    /// </summary>
    [TestMethod]
    public void Verify_WhenParameterHasNoEquals_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Scrypt.Verify("$scrypt$noequals$c2FsdA$aGFzaA", Password));
    }

    /// <summary>
    /// Verifies that decoding a PHC string with an unknown parameter key throws.
    /// </summary>
    [TestMethod]
    public void Verify_WhenParameterKeyUnknown_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Scrypt.Verify("$scrypt$x=5$c2FsdA$aGFzaA", Password));
    }

    /// <summary>
    /// Verifies that decoding a PHC string whose log-N is out of range throws.
    /// </summary>
    [TestMethod]
    public void Verify_WhenLogNOutOfRange_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Scrypt.Verify("$scrypt$ln=99,r=8,p=1$c2FsdA$aGFzaA", Password));
    }
}
