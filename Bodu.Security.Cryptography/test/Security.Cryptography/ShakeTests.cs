// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Shake" /> extendable output function.
/// </summary>
[TestClass]
public partial class ShakeTests
    : HashAlgorithmTests<ShakeTests, Shake, ShakeTests.ShakeVariant>
{
    /// <summary>Identifies the security-level variants of <see cref="Shake" />.</summary>
    public enum ShakeVariant
    {
        /// <summary>SHAKE128 (security level = 128 bits, rate = 168 bytes, 256-bit default output).</summary>
        Shake128,

        /// <summary>SHAKE256 (security level = 256 bits, rate = 136 bytes, 256-bit default output).</summary>
        Shake256,
    }

    /// <inheritdoc />
    protected override ShakeVariant DefaultVariant => ShakeVariant.Shake128;

    /// <inheritdoc />
    public override IEnumerable<ShakeVariant> GetHashAlgorithmVariants() =>
    [
        ShakeVariant.Shake128,
        ShakeVariant.Shake256,
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(ShakeVariant variant) =>
        variant switch
        {
            ShakeVariant.Shake128 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                InputBlockSize = 168,
                OutputBlockSize = 32,
                LongInputLength = 336,
                BoundaryLengths = [1, 167, 168, 169, 336],
                MinNonZeroBytesForLongInput = 30,
            },
            ShakeVariant.Shake256 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                InputBlockSize = 136,
                OutputBlockSize = 32,
                LongInputLength = 272,
                BoundaryLengths = [1, 135, 136, 137, 272],
                MinNonZeroBytesForLongInput = 30,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Shake CreateAlgorithm(ShakeVariant variant) =>
        variant switch
        {
            ShakeVariant.Shake128 => new Shake(256, 128),
            ShakeVariant.Shake256 => new Shake(256, 256),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(ShakeVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(ShakeVariant variant) =>
        variant switch
        {
            // Known-answer test vectors from NIST FIPS 202 sample messages (256-bit output).
            ShakeVariant.Shake128 => new Dictionary<string, string>
            {
                ["Empty"] = "7F9C2BA4E88F827D616045507605853ED73B8093F6EFBC88EB1A6EACFA66EF26",
            },
            ShakeVariant.Shake256 => new Dictionary<string, string>
            {
                ["Empty"] = "46B9DD2B0BA88D13233B3FEB743EEB243FCC52EA62BA81B98BD2AE201963F48F",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <summary>
    /// Verifies that requesting an invalid security level throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSecurityLevelIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Shake(256, 192);
        });
    }

    /// <summary>
    /// Verifies that requesting a non-positive output size throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOutputBitsIsNotPositiveMultipleOf8_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Shake(0, 128);
        });
    }

    /// <summary>
    /// Verifies that SHAKE128 and SHAKE256 produce different digests for the same input and output length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSecurityLevelsDiffer_ShouldProduceDifferentHashes()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        using Shake shake128 = new(256, 128);
        using Shake shake256 = new(256, 256);

        byte[] hash128 = shake128.ComputeHash(input);
        byte[] hash256 = shake256.ComputeHash(input);

        CollectionAssert.AreNotEqual(hash128, hash256,
            "SHAKE128 and SHAKE256 must produce different digests for the same input.");
    }

    /// <summary>
    /// Verifies that the security level property returns the value supplied at construction time.
    /// </summary>
    [TestMethod]
    public void SecurityLevel_AfterConstruction_ShouldReturnSuppliedValue()
    {
        using Shake sut128 = new(256, 128);
        using Shake sut256 = new(256, 256);

        Assert.AreEqual(128, sut128.SecurityLevel);
        Assert.AreEqual(256, sut256.SecurityLevel);
    }
}
