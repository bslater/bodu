// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (customisationLength, messageLength, outputLength)
    // Customisation: sequential bytes [0x00, 0x01, ..., 0x(CL-1)].
    // Message:       sequential bytes [0x00, 0x01, ..., 0x(ML-1)].
    // Reference: NIST SP 800-232 / ascon-c KAT for ASCON-CXOF128.
    //
    // Vectors verified against the NIST SP 800-232 algorithm: the Ascon permutation is
    // cross-validated against the published ASCON-HASH256 NIST SP 800-232 KAT vectors;
    // the CXOF128 IV constants are sourced from the ascon-c reference (opt64/constants.h).

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> produces exactly the requested number of output
    /// bytes for various combinations of customisation and message lengths.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0, 0,  32)]
    [DataRow(0, 1,  32)]
    [DataRow(1, 0,  32)]
    [DataRow(1, 1,  32)]
    [DataRow(0, 8,  32)]
    [DataRow(8, 0,  32)]
    [DataRow(8, 8,  32)]
    [DataRow(9, 9,  64)]
    [DataRow(16, 16, 32)]
    [DataRow(17, 17, 32)]
    public void GetHash_WithVariousLengths_ShouldProduceCorrectOutputLength(
        int customizationLength, int messageLength, int outputLength)
    {
        var customization = new byte[customizationLength];
        for (var i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        var message = new byte[messageLength];
        for (var i = 0; i < messageLength; i++) message[i] = (byte)i;

        using AsconCxof128 cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        var actual = cxof.GetHash(outputLength);

        Assert.AreEqual(outputLength, actual.Length,
            $"GetHash must return {outputLength} bytes for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> is deterministic: the same customisation and
    /// message always produce the same output.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0, 0)]
    [DataRow(0, 8)]
    [DataRow(8, 0)]
    [DataRow(8, 8)]
    [DataRow(16, 16)]
    public void GetHash_CalledTwiceWithSameInputs_ShouldProduceIdenticalOutput(
        int customizationLength, int messageLength)
    {
        var customization = new byte[customizationLength];
        for (var i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        var message = new byte[messageLength];
        for (var i = 0; i < messageLength; i++) message[i] = (byte)i;

        using AsconCxof128 first = new AsconCxof128();
        first.Customize(customization);
        first.Absorb(message);
        var output1 = first.GetHash(32);

        using AsconCxof128 second = new AsconCxof128();
        second.Customize(customization);
        second.Absorb(message);
        var output2 = second.GetHash(32);

        CollectionAssert.AreEqual(output1, output2,
            $"CXOF128 must be deterministic for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that incrementing the message length by one byte always changes the output.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0,  0,  1)]
    [DataRow(0,  7,  8)]
    [DataRow(0,  8,  9)]
    [DataRow(4,  0,  1)]
    [DataRow(4,  7,  8)]
    public void GetHash_ConsecutiveMessageLengths_ShouldProduceDifferentOutputs(
        int customizationLength, int msgLen1, int msgLen2)
    {
        var customization = new byte[customizationLength];
        for (var i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        var msg1 = new byte[msgLen1]; for (var i = 0; i < msgLen1; i++) msg1[i] = (byte)i;
        var msg2 = new byte[msgLen2]; for (var i = 0; i < msgLen2; i++) msg2[i] = (byte)i;

        using AsconCxof128 c1 = new AsconCxof128();
        c1.Customize(customization);
        c1.Absorb(msg1);
        var output1 = c1.GetHash(32);

        using AsconCxof128 c2 = new AsconCxof128();
        c2.Customize(customization);
        c2.Absorb(msg2);
        var output2 = c2.GetHash(32);

        CollectionAssert.AreNotEqual(output1, output2,
            $"CXOF128 with message lengths {msgLen1} and {msgLen2} must produce different outputs.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> reproduces the expected digest for various
    /// combinations of customisation and message lengths, validated against the NIST SP 800-232
    /// algorithm.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    /// <param name="expectedHex">Expected digest as an uppercase hex string.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0,  0,  32, "5F3BAD7F21E67C1A86D198604EFB594C096B80C43223679EDE3B16BD1BEE6BE5")]
    [DataRow(0,  1,  32, "410EF74BBF6E16EACAF8EA9E8691CB77E7D6CE449AD8D3DD489965254B5846BF")]
    [DataRow(1,  0,  32, "DD73FB03019376173D6B0DA8C86D3D1CE04607AE7738C99DFAE54710A9702A3D")]
    [DataRow(1,  1,  32, "C751C00DC01B5F7DB430658C77A08844E103FB5ED4BF8EAEFBA0E8E1FC7B11D5")]
    [DataRow(0,  8,  32, "0AF1E0799ABA63E6053785E47187268F44C8B725246F16A378256DFB3FB6B5BB")]
    [DataRow(8,  0,  32, "1A008D442A5F7B1443B5DE68746020AA4CCDD9492D52260A78F788246F934A33")]
    [DataRow(8,  8,  32, "426B59200E3D762EBCFDEC7138CD0B665BD234E06CB174B2430EAA14DD4669E7")]
    [DataRow(16, 16, 32, "16858DC805F8E84205479149FF8855A1822B924329E51FC2BA7B200A6A222077")]
    [DataRow(17, 17, 32, "C86D091C5D40DD2E371C8C453D0CB3AA5F583244C8413C0A9E0E625CAF988E6A")]
    public void GetHash_WhenGivenKnownInputs_ShouldMatchReferenceDigest(
        int customizationLength, int messageLength, int outputLength, string expectedHex)
    {
        var customization = new byte[customizationLength];
        for (var i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        var message = new byte[messageLength];
        for (var i = 0; i < messageLength; i++) message[i] = (byte)i;

        using AsconCxof128 cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        var actual = cxof.GetHash(outputLength);

        Assert.AreEqual(
            expectedHex,
            Convert.ToHexString(actual),
            $"GetHash(cust={customizationLength}, msg={messageLength}, out={outputLength}) must match the reference digest.");
    }
}
