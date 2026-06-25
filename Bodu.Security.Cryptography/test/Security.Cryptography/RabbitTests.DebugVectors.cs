// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTests.DebugVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;
/// <summary>
/// Validates the <see cref="Rabbit" /> internal state against the RFC 4503 Appendix B debugging vectors (key setup and
/// IV setup), confirming the core key expansion, next-state function, counter carries, and final XOR independently of
/// the keystream serialization. The Appendix B printed key has a typo (byte 2 = ED); the expanded state proves it is 3D.
/// </summary>
public sealed partial class RabbitTests
{
    /// <summary>
    /// One expected inner-state checkpoint from RFC 4503 Appendix B.
    /// </summary>
    private sealed record StateCheckpoint(string Name, uint Carry, uint[] X, uint[] C);

    private const string AppendixBKeyHex = "912813292E3D36FE3BFC62F1DC51C3AC";
    private const string AppendixBIvHex = "C373F575C1267E59";

    // RFC 4503 Appendix B.1 — key setup checkpoints, then the post-48-byte-output state.
    private static readonly StateCheckpoint[] KeySetupCheckpoints =
    [
        new("Inner state after key expansion", 0,
            [0xDC51C3AC, 0x13292E3D, 0x3BFC62F1, 0xC3AC9128, 0x2E3D36FE, 0x62F1DC51, 0x91281329, 0x36FE3BFC],
            [0x36FE2E3D, 0xDC5162F1, 0x13299128, 0x3BFC36FE, 0xC3ACDC51, 0x2E3D1329, 0x62F13BFC, 0x9128C3AC]),
        new("Inner state after first key setup iteration", 1,
            [0xF2E8C8B1, 0x38E06FA7, 0x9A0D72C0, 0xF21F5334, 0xCACDCCC3, 0x4B239CBE, 0x0565DCCC, 0xB1587C8D],
            [0x8433018A, 0xAF9E97C4, 0x47FCDE5D, 0x89310A4B, 0x96FA1124, 0x6310605E, 0xB0260F49, 0x6475F87F]),
        new("Inner state after fourth key setup iteration", 0,
            [0x1D059312, 0xBDDC3E45, 0xF440927D, 0x50CBB553, 0x36709423, 0x0B6F0711, 0x3ADA3A7B, 0xEB9800C8],
            [0x6BD17B74, 0x2986363E, 0xE676C5FC, 0x70CF8432, 0x10E1AF9E, 0x018A47FD, 0x97C48931, 0xDE5D96F9]),
        new("Inner state after final key setup xor", 0,
            [0x1D059312, 0xBDDC3E45, 0xF440927D, 0x50CBB553, 0x36709423, 0x0B6F0711, 0x3ADA3A7B, 0xEB9800C8],
            [0x5DA1EF57, 0x22E9312F, 0xDCACFF87, 0x9B5784FA, 0x0DE43C8C, 0xBC5679B8, 0x63841B4C, 0x8E9623AA]),
        new("Inner state after generation of 48 bytes of output", 1,
            [0xB5428566, 0xA2593617, 0xFF5578DE, 0x7293950F, 0x145CE109, 0xC93875B0, 0xD34306E0, 0x43FEEF87],
            [0x45406940, 0x9CD0CFA9, 0x7B26E725, 0x82F5FEE2, 0x87CBDB06, 0x5AD06156, 0x4B229534, 0x087DC224]),
    ];

    // RFC 4503 Appendix B.2 — IV setup checkpoints.
    private static readonly StateCheckpoint[] IvSetupCheckpoints =
    [
        new("Inner state after IV expansion", 0,
            [0x1D059312, 0xBDDC3E45, 0xF440927D, 0x50CBB553, 0x36709423, 0x0B6F0711, 0x3ADA3A7B, 0xEB9800C8],
            [0x9C87910E, 0xE19AF009, 0x1FDF0AF2, 0x6E22FAA3, 0xCCC242D5, 0x7F25B89E, 0xA0F7EE39, 0x7BE35DF3]),
        new("Inner state after first IV setup iteration", 1,
            [0xC4FF831A, 0xEF5CD094, 0xC5933855, 0xC05A5C03, 0x4A50522F, 0xDF487BE4, 0xA45FA013, 0x05531179],
            [0xE9BC645B, 0xB4E824DC, 0x54B25827, 0xBB57CDF0, 0xA00F77A8, 0xB3F905D3, 0xEE2CC186, 0x4F3092C6]),
        new("Inner state after fourth IV setup iteration", 1,
            [0x6274E424, 0xE14CE120, 0xDA8739D9, 0x65E0402D, 0xD1281D10, 0xBD435BAA, 0x4E9E7A02, 0x9B467ABD],
            [0xD15ADE44, 0x2ECFC356, 0xF32C3FC6, 0xA2F647D7, 0x19F71622, 0x5272ED72, 0xD5CB3B6E, 0xC9183140]),
    ];

    /// <summary>
    /// Verifies that the Rabbit inner state during key setup matches the RFC 4503 Appendix B.1 checkpoints, including
    /// the post-output state after 48 keystream bytes.
    /// </summary>
    [TestMethod]
    public void KeySetup_WhenUsingRfc4503AppendixB1Vector_ShouldMatchInternalStateCheckpoints()
    {
        IReadOnlyList<RabbitStreamCipher.StateCheckpoint> actual =
            RabbitStreamCipher.TraceKeySetup(Convert.FromHexString(AppendixBKeyHex));

        AssertCheckpoints(KeySetupCheckpoints, actual);
    }

    /// <summary>
    /// Verifies that the Rabbit inner state during IV setup matches the RFC 4503 Appendix B.2 checkpoints.
    /// </summary>
    [TestMethod]
    public void IvSetup_WhenUsingRfc4503AppendixB2Vector_ShouldMatchInternalStateCheckpoints()
    {
        IReadOnlyList<RabbitStreamCipher.StateCheckpoint> actual = RabbitStreamCipher.TraceIvSetup(
            Convert.FromHexString(AppendixBKeyHex), Convert.FromHexString(AppendixBIvHex));

        AssertCheckpoints(IvSetupCheckpoints, actual);
    }

    /// <summary>
    /// Asserts that every captured inner-state checkpoint matches the expected RFC 4503 Appendix B values.
    /// </summary>
    /// <param name="expected">The expected checkpoints.</param>
    /// <param name="actual">The checkpoints captured from the engine.</param>
    private static void AssertCheckpoints(
        StateCheckpoint[] expected, IReadOnlyList<RabbitStreamCipher.StateCheckpoint> actual)
    {
        Assert.HasCount(expected.Length, actual, "Unexpected number of inner-state checkpoints.");

        for (int i = 0; i < expected.Length; i++)
        {
            StateCheckpoint want = expected[i];
            RabbitStreamCipher.StateCheckpoint got = actual[i];

            Assert.AreEqual(want.Name, got.Name);
            Assert.AreEqual(want.Carry, got.Carry, $"{want.Name}: carry");
            CollectionAssert.AreEqual(want.X, got.X, $"{want.Name}: X state");
            CollectionAssert.AreEqual(want.C, got.C, $"{want.Name}: C counters");
        }
    }
}
