// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (inputLength, outputLength, expectedHex)
    // Message: sequential bytes [0x00, 0x01, ..., 0x(N-1)].
    // Reference: ascon-c LWC_XOF_KAT_128_512 (NIST SP 800-232 Ascon-XOF128).
    //
    // These digests are the exact ascon-c reference outputs: for an N-byte sequential message the
    // XOF output is the prefix of the file's 512-bit MD for the row whose Msg is that sequence.
    // Because XOF output is a prefix, a 32-byte digest is the first 32 bytes of the 64-byte MD.

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces exactly the requested number of
    /// output bytes for sequential byte inputs of various lengths.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]

    [DataRow(0, 32)]
    [DataRow(0, 64)]
    [DataRow(1, 32)]
    [DataRow(7, 32)]
    [DataRow(8, 32)]
    [DataRow(9, 32)]
    [DataRow(16, 32)]
    [DataRow(17, 32)]
    [DataRow(64, 64)]
    public void HashData_WithVariousInputLengths_ShouldProduceCorrectOutputLength(
        int inputLength, int outputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] actual = AsconXof128.HashData(message, outputLength);

        Assert.HasCount(outputLength, actual,
            $"HashData({inputLength}, {outputLength}) must return exactly {outputLength} bytes.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> is deterministic: calling it twice with
    /// the same input and output length must return identical byte sequences.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    [TestMethod]

    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(16)]
    public void HashData_CalledTwiceWithSameInput_ShouldProduceIdenticalOutput(int inputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] first = AsconXof128.HashData(message, 32);
        byte[] second = AsconXof128.HashData(message, 32);

        CollectionAssert.AreEqual(first, second,
            $"HashData must be deterministic for {inputLength}-byte sequential input.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces different outputs for consecutive
    /// input lengths — i.e., adding one byte to the message always changes the output.
    /// </summary>
    [TestMethod]

    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(7, 8)]
    [DataRow(8, 9)]
    [DataRow(15, 16)]
    [DataRow(16, 17)]
    public void HashData_ConsecutiveInputLengths_ShouldProduceDifferentOutputs(int len1, int len2)
    {
        byte[] msg1 = new byte[len1]; for (int i = 0; i < len1; i++) msg1[i] = (byte)i;
        byte[] msg2 = new byte[len2]; for (int i = 0; i < len2; i++) msg2[i] = (byte)i;

        byte[] output1 = AsconXof128.HashData(msg1, 32);
        byte[] output2 = AsconXof128.HashData(msg2, 32);

        CollectionAssert.AreNotEqual(output1, output2,
            $"HashData for {len1}-byte and {len2}-byte inputs must produce different outputs.");
    }

    /// <summary>
    /// Resource name of the embedded ascon-c <c>LWC_XOF_KAT_128_512</c> reference file.
    /// </summary>
    private const string KatResourceName = "Bodu.Security.Cryptography.AsconXof128.LWC_XOF_KAT_128_512.txt";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string KatSource = "NIST SP 800-232 / ascon-c LWC_XOF_KAT_128_512";

    /// <summary>
    /// Loads every Ascon-XOF128 known-answer vector embedded in the test assembly and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per KAT vector; each row contains a single <see cref="XofKnownAnswer" /> object.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static IEnumerable<object[]> AsconXof128ReferenceVectors()
    {
        using Stream stream = typeof(AsconXof128Tests).Assembly.GetManifestResourceStream(KatResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{KatResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (XofKnownAnswer vector in NistLwcXofKatReader.Read(stream, KatSource))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the source file's <c>Count</c>.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="XofKnownAnswer" />).</param>
    /// <returns>A short label identifying this KAT vector.</returns>
    public static string GetKatVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is XofKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> reproduces, byte-for-byte, the exact output of every vector in
    /// the official ascon-c <c>LWC_XOF_KAT_128_512</c> reference file — the full 1025-row corpus, loaded dynamically
    /// from the embedded resource, pinning the entire data path to the published NIST SP 800-232 KAT.
    /// </summary>
    /// <param name="vector">The XOF known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(AsconXof128ReferenceVectors), DynamicDataDisplayName = nameof(GetKatVectorDisplayName))]
    [TestCategory("Regression")]
    public void HashData_WhenGivenNistXofKatVector_ShouldMatchReferenceOutput(XofKnownAnswer vector)
    {
        byte[] actual = AsconXof128.HashData(vector.Message, vector.Digest.Length);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector}: Ascon-XOF128 output must match the {KatSource} reference vector.");
    }
}
