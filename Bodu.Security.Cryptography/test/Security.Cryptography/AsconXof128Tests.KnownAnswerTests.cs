// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (inputLength, outputLength, expectedHex)
    // Message: sequential bytes [0x00, 0x01, ..., 0x(N-1)].
    // Reference: NIST SP 800-232 / ascon-c LWC_HASH_KAT for ASCON-XOF128.
    //
    // IMPORTANT: The hex values below must be verified against the ascon-c reference
    // implementation (LWC_HASH_KAT_256_XOF.txt or equivalent) before relying on them as a
    // regression baseline. Run the reference implementation, capture outputs, and replace the
    // placeholder values here. Until then, these tests exercise output-length correctness and
    // determinism only.

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces exactly the requested number of
    /// output bytes for sequential byte inputs of various lengths.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]
    [DataRow(0,  32)]
    [DataRow(0,  64)]
    [DataRow(1,  32)]
    [DataRow(7,  32)]
    [DataRow(8,  32)]
    [DataRow(9,  32)]
    [DataRow(16, 32)]
    [DataRow(17, 32)]
    [DataRow(64, 64)]
    public void HashData_WithVariousInputLengths_ShouldProduceCorrectOutputLength(
        int inputLength, int outputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] actual = AsconXof128.HashData(message, outputLength);

        Assert.AreEqual(outputLength, actual.Length,
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

        byte[] first  = AsconXof128.HashData(message, 32);
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
}
