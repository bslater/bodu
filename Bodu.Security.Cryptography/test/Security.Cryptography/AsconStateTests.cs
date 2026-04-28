// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconStateTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconState" /> permutation state struct.
/// </summary>
[TestClass]
public class AsconStateTests
{
    // ── Clear ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconState.Clear" /> sets all five state words to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledOnNonZeroState_ShouldZeroAllWords()
    {
        AsconState state = new AsconState { S0 = 1, S1 = 2, S2 = 3, S3 = 4, S4 = 5 };
        state.Clear();

        Assert.AreEqual(0UL, state.S0);
        Assert.AreEqual(0UL, state.S1);
        Assert.AreEqual(0UL, state.S2);
        Assert.AreEqual(0UL, state.S3);
        Assert.AreEqual(0UL, state.S4);
    }

    // ── Permute ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconState.Permute" /> changes the state — a non-zero input must
    /// not produce the same values after a permutation (probabilistic sanity check).
    /// </summary>
    [TestMethod]
    public void Permute_WhenCalledOnNonZeroState_ShouldModifyState()
    {
        AsconState state = new AsconState { S0 = 0xDEADBEEFCAFEBABEUL, S1 = 1, S2 = 2, S3 = 3, S4 = 4 };
        ulong originalS0 = state.S0;

        state.Permute(12);

        Assert.AreNotEqual(originalS0, state.S0, "Permute(12) must alter at least S0 on a non-trivial input.");
    }

    /// <summary>
    /// Verifies that applying the ASCON permutation twice with the same round count produces
    /// different output from a single application — i.e., the permutation is not self-inverse.
    /// </summary>
    [TestMethod]
    public void Permute_AppliedTwice_ShouldProduceDifferentStateFromOnce()
    {
        AsconState once = new AsconState { S0 = 0x0102030405060708UL, S1 = 0, S2 = 0, S3 = 0, S4 = 0 };
        AsconState twice = once;

        once.Permute(8);
        twice.Permute(8);
        twice.Permute(8);

        bool allSame = once.S0 == twice.S0 && once.S1 == twice.S1 && once.S2 == twice.S2
                    && once.S3 == twice.S3 && once.S4 == twice.S4;
        Assert.IsFalse(allSame, "Permute applied twice must differ from a single application.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconState.Permute" /> with different round counts produces
    /// different results from the same initial state. Ascon-p8 and Ascon-p12 must not agree.
    /// </summary>
    [TestMethod]
    public void Permute_DifferentRoundCounts_ShouldProduceDifferentResults()
    {
        AsconState p8  = new AsconState { S0 = 0x01UL, S1 = 0x02UL, S2 = 0x03UL, S3 = 0x04UL, S4 = 0x05UL };
        AsconState p12 = p8;

        p8.Permute(8);
        p12.Permute(12);

        bool allSame = p8.S0 == p12.S0 && p8.S1 == p12.S1 && p8.S2 == p12.S2
                    && p8.S3 == p12.S3 && p8.S4 == p12.S4;
        Assert.IsFalse(allSame, "Permute(8) and Permute(12) must yield different results.");
    }

    // ── AbsorbRate64 / SqueezeRate64 ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconState.AbsorbRate64" /> XORs the input bytes into <c>S0</c>
    /// using little-endian byte order and leaves <c>S1</c>–<c>S4</c> unchanged.
    /// </summary>
    [TestMethod]
    public void AbsorbRate64_WhenAbsorbingBytes_ShouldXorIntoS0LittleEndian()
    {
        AsconState state = new AsconState { S0 = 0, S1 = 0xFFFFFFFFFFFFFFFFUL, S2 = 0, S3 = 0, S4 = 0 };
        byte[] block = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        state.AbsorbRate64(block);

        // Little-endian: bytes [01 02 03 04 05 06 07 08] → 0x0807060504030201
        Assert.AreEqual(0x0807060504030201UL, state.S0, "AbsorbRate64 must read the block as little-endian into S0.");
        Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, state.S1, "AbsorbRate64 must not modify S1.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconState.SqueezeRate64" /> writes <c>S0</c> into the output
    /// buffer as a little-endian 64-bit word.
    /// </summary>
    [TestMethod]
    public void SqueezeRate64_WhenCalled_ShouldWriteS0AsLittleEndian()
    {
        AsconState state = new AsconState { S0 = 0x0807060504030201UL };
        byte[] dest = new byte[8];

        state.SqueezeRate64(dest);

        byte[] expected = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        CollectionAssert.AreEqual(expected, dest, "SqueezeRate64 must write S0 as little-endian bytes.");
    }

    // ── AbsorbRate128 / SqueezeRate128 ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconState.AbsorbRate128" /> XORs the input bytes into
    /// <c>S0</c> and <c>S1</c> using little-endian byte order and leaves <c>S2</c>–<c>S4</c>
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void AbsorbRate128_WhenAbsorbingBytes_ShouldXorIntoS0AndS1LittleEndian()
    {
        AsconState state = new AsconState { S0 = 0, S1 = 0, S2 = 0xFFFFFFFFFFFFFFFFUL, S3 = 0, S4 = 0 };
        byte[] block = new byte[16];
        block[0] = 0x01; // LE: S0 low byte
        block[8] = 0x02; // LE: S1 low byte

        state.AbsorbRate128(block);

        Assert.AreEqual(0x0000000000000001UL, state.S0, "AbsorbRate128 must XOR first 8 bytes as LE into S0.");
        Assert.AreEqual(0x0000000000000002UL, state.S1, "AbsorbRate128 must XOR next 8 bytes as LE into S1.");
        Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, state.S2, "AbsorbRate128 must not modify S2.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconState.SqueezeRate128" /> writes <c>S0</c> and <c>S1</c>
    /// into the output buffer as consecutive little-endian 64-bit words.
    /// </summary>
    [TestMethod]
    public void SqueezeRate128_WhenCalled_ShouldWriteS0AndS1AsLittleEndian()
    {
        AsconState state = new AsconState { S0 = 0x0807060504030201UL, S1 = 0x100F0E0D0C0B0A09UL };
        byte[] dest = new byte[16];

        state.SqueezeRate128(dest);

        byte[] expected =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
        ];
        CollectionAssert.AreEqual(expected, dest, "SqueezeRate128 must write S0||S1 as little-endian bytes.");
    }

    // ── Permutation known-answer test (cross-verification with AsconHash256) ──────────────────

    /// <summary>
    /// Verifies that the ASCON permutation produces the correct known-answer output for the
    /// AsconHash256 pre-image of the empty string. The empty-string hash must equal the value
    /// established by the NIST SP 800-232 / ascon-c reference implementation.
    /// </summary>
    /// <remarks>
    /// This test exercises the full permutation path indirectly by confirming that the shared
    /// <see cref="AsconState" /> permutation, when used via <see cref="AsconHash256" />, produces
    /// the correct NIST-standardised output. A failure here indicates a regression in the
    /// permutation, IV loading, or rate-absorption helpers.
    /// </remarks>
    [TestMethod]
    public void Permute_ViaAsconHash256EmptyInput_ShouldMatchNistKat()
    {
        using AsconHash256 hash = new AsconHash256();
        byte[] digest = hash.ComputeHash(Array.Empty<byte>());

        string hex = Convert.ToHexString(digest);
        Assert.AreEqual(
            "0B3BE5850F2F6B98CAF29F8FDEA89B64A1FA70AA249B8F839BD53BAA304D92B2",
            hex,
            ignoreCase: true,
            "AsconHash256 of empty input must match NIST SP 800-232 KAT.");
    }
}
