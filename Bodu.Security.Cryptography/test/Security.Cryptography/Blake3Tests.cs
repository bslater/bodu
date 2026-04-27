// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Blake3" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Blake3Tests
    : HashAlgorithmTests<Blake3Tests, Blake3, Blake3Tests.Blake3Variant>
{
    /// <summary>Identifies the single configuration variant of <see cref="Blake3" />.</summary>
    public enum Blake3Variant
    {
        /// <summary>The standard BLAKE3 configuration producing a 256-bit digest.</summary>
        Default,
    }

    private static readonly HashAlgorithmSpecification Specification = new()
    {
        HashSize = 256,
        InputBlockSize = 1024,
        OutputBlockSize = 32,
        LongInputLength = 2048,
        BoundaryLengths = [1, 512, 1024, 1025, 2048],
        MinNonZeroBytesForLongInput = 28,
    };

    /// <inheritdoc />
    public override IEnumerable<Blake3Variant> GetHashAlgorithmVariants() => [Blake3Variant.Default];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Blake3Variant variant) => Specification;

    /// <inheritdoc />
    protected override Blake3 CreateAlgorithm(Blake3Variant variant) => new Blake3();

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Blake3Variant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Blake3Variant variant) =>
        new Dictionary<string, string>
        {
            // Known-answer test vectors from the official BLAKE3 test_vectors.json (seed = 0, 256-bit output).
            ["Empty"] = "AF1349B9F5F9A1A6A0404DEA36DCC9499BCB25C9ADC112B7CC9A93CAE41F3262",
            ["ABC"] = "D1717274597CF0289694F75D96D444B992A096F1AFD8E7BBFA6EBB1D360FEDFC",
            ["Zeros_16"] = "E572DFF82304700B856A555AC3A4558D0DF3646A3727816500270A93C66AAC1E",
            ["Sequential_0_255"] = "EF2017870B6890925E1CF4DB3C3E9FB300C4CA325BA6B1400A41DE3630524C1A",
        };

    /// <summary>
    /// Verifies that the digest length is exactly 32 bytes (256 bits) for any input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_ForAnyInput_ShouldReturnThirtyTwoBytes()
    {
        using Blake3 sut = new();
        byte[] hash = sut.ComputeHash(System.Text.Encoding.ASCII.GetBytes("hello BLAKE3"));

        Assert.AreEqual(32, hash.Length, "BLAKE3 must always produce a 32-byte (256-bit) digest.");
    }

    /// <summary>
    /// Verifies that two inputs that span multiple 1024-byte chunks produce different hashes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_ForMultiChunkInputs_ShouldProduceDistinctHashes()
    {
        byte[] inputA = new byte[2048];
        byte[] inputB = new byte[2048];
        inputB[1000] = 0xFF;

        using Blake3 sut = new();
        byte[] hashA = sut.ComputeHash(inputA);
        byte[] hashB = sut.ComputeHash(inputB);

        CollectionAssert.AreNotEqual(hashA, hashB,
            "Distinct multi-chunk inputs must produce different hashes.");
    }

    /// <summary>
    /// Verifies that single-pass and streaming input (fed byte-by-byte) produce identical digests.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsStreamedByteByByte_ShouldMatchSinglePassHash()
    {
        byte[] input = new byte[1500];
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)(i & 0xFF);

        using Blake3 single = new();
        byte[] expected = single.ComputeHash(input);

        using Blake3 streaming = new();
        streaming.Initialize();
        foreach (byte b in input)
            streaming.TransformBlock(new[] { b }, 0, 1, null, 0);
        streaming.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] actual = streaming.Hash!;

        CollectionAssert.AreEqual(expected, actual,
            "Streaming (byte-by-byte) and single-pass hashing must produce identical results.");
    }
}
