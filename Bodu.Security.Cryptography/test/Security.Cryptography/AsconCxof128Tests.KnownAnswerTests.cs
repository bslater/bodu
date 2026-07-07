// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (customisationLength, messageLength, outputLength)
    // Customisation: sequential bytes [0x00, 0x01, ..., 0x(CL-1)].
    // Message:       sequential bytes [0x00, 0x01, ..., 0x(ML-1)].
    // Reference: NIST SP 800-232 Ascon-CXOF128, cross-checked against the ascon-c
    // LWC_CXOF_KAT_128_512 reference file (all 1089 rows reproduce; the empty-customisation rows
    // here equal the file's empty-Z counterparts truncated to the requested output length).

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> produces exactly the requested number of output
    /// bytes for various combinations of customisation and message lengths.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]

    [DataRow(0, 0, 32)]
    [DataRow(0, 1, 32)]
    [DataRow(1, 0, 32)]
    [DataRow(1, 1, 32)]
    [DataRow(0, 8, 32)]
    [DataRow(8, 0, 32)]
    [DataRow(8, 8, 32)]
    [DataRow(9, 9, 64)]
    [DataRow(16, 16, 32)]
    [DataRow(17, 17, 32)]
    public void GetHash_WithVariousLengths_ShouldProduceCorrectOutputLength(
        int customizationLength, int messageLength, int outputLength)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] message = new byte[messageLength];
        for (int i = 0; i < messageLength; i++) message[i] = (byte)i;

        using var cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        byte[] actual = cxof.GetHash(outputLength);

        Assert.HasCount(outputLength, actual,
            $"GetHash must return {outputLength} bytes for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> is deterministic: the same customisation and
    /// message always produce the same output.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    [TestMethod]

    [DataRow(0, 0)]
    [DataRow(0, 8)]
    [DataRow(8, 0)]
    [DataRow(8, 8)]
    [DataRow(16, 16)]
    public void GetHash_CalledTwiceWithSameInputs_ShouldProduceIdenticalOutput(
        int customizationLength, int messageLength)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] message = new byte[messageLength];
        for (int i = 0; i < messageLength; i++) message[i] = (byte)i;

        using var first = new AsconCxof128();
        first.Customize(customization);
        first.Absorb(message);
        byte[] output1 = first.GetHash(32);

        using var second = new AsconCxof128();
        second.Customize(customization);
        second.Absorb(message);
        byte[] output2 = second.GetHash(32);

        CollectionAssert.AreEqual(output1, output2,
            $"CXOF128 must be deterministic for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that incrementing the message length by one byte always changes the output.
    /// </summary>
    [TestMethod]

    [DataRow(0, 0, 1)]
    [DataRow(0, 7, 8)]
    [DataRow(0, 8, 9)]
    [DataRow(4, 0, 1)]
    [DataRow(4, 7, 8)]
    public void GetHash_ConsecutiveMessageLengths_ShouldProduceDifferentOutputs(
        int customizationLength, int msgLen1, int msgLen2)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] msg1 = new byte[msgLen1]; for (int i = 0; i < msgLen1; i++) msg1[i] = (byte)i;
        byte[] msg2 = new byte[msgLen2]; for (int i = 0; i < msgLen2; i++) msg2[i] = (byte)i;

        using var c1 = new AsconCxof128();
        c1.Customize(customization);
        c1.Absorb(msg1);
        byte[] output1 = c1.GetHash(32);

        using var c2 = new AsconCxof128();
        c2.Customize(customization);
        c2.Absorb(msg2);
        byte[] output2 = c2.GetHash(32);

        CollectionAssert.AreNotEqual(output1, output2,
            $"CXOF128 with message lengths {msgLen1} and {msgLen2} must produce different outputs.");
    }

    /// <summary>
    /// Resource name of the embedded ascon-c <c>LWC_CXOF_KAT_128_512</c> reference file.
    /// </summary>
    private const string KatResourceName = "Bodu.Security.Cryptography.AsconCxof128.LWC_CXOF_KAT_128_512.txt";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string KatSource = "NIST SP 800-232 / ascon-c LWC_CXOF_KAT_128_512";

    /// <summary>
    /// Loads every Ascon-CXOF128 known-answer vector embedded in the test assembly and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per KAT vector; each row contains a single <see cref="XofKnownAnswer" /> object.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static IEnumerable<object[]> AsconCxof128ReferenceVectors()
    {
        using Stream stream = typeof(AsconCxof128Tests).Assembly.GetManifestResourceStream(KatResourceName)
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
    /// Verifies that <see cref="AsconCxof128" /> reproduces, byte-for-byte, the exact output of every vector in the
    /// official ascon-c <c>LWC_CXOF_KAT_128_512</c> reference file — the full 1089-row corpus, loaded dynamically from
    /// the embedded resource. The file's customisation <c>Z</c> begins at <c>0x10</c>, so this exercises the SP 800-232
    /// length-prefixed customisation-absorb path across the entire published KAT.
    /// </summary>
    /// <param name="vector">The CXOF known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(AsconCxof128ReferenceVectors), DynamicDataDisplayName = nameof(GetKatVectorDisplayName))]
    [TestCategory("Regression")]
    public void GetHash_WhenGivenNistCxofKatVector_ShouldMatchReferenceOutput(XofKnownAnswer vector)
    {
        using var cxof = new AsconCxof128();
        cxof.Customize(vector.Customization ?? []);
        cxof.Absorb(vector.Message);
        byte[] actual = cxof.GetHash(vector.Digest.Length);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector}: Ascon-CXOF128 output must match the {KatSource} reference vector.");
    }
}
