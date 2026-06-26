// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

// Known-answer vectors — Bellare, Rogaway and Wagner, "The EAX Mode of Operation" (FSE 2004),
// Appendix G. All vectors use AES-128 with a 16-byte nonce, 8-byte header (AAD), and
// ciphertext encoded as C || T (ciphertext followed by the 16-byte authentication tag).
//
// Source: https://web.cs.ucdavis.edu/~rogaway/papers/eax.pdf
public sealed partial class EaxModeTransformTests
{
    private static readonly AeadKnownAnswer[] KnownAnswers =
    [
        // Vector 1 — empty message
        Eax("EAX paper App. G #1 — empty message",
            keyHex: "233952DEE4D5ED5F9B9C6D6FF80FF478",
            nonceHex: "62EC67F9C3A4A407FCB2A8C49031A8B3",
            aadHex: "6BFB914FD07EAE6B",
            ptHex: "",
            ctHex: "",
            tagHex: "E037830E8389F27B025A2D6527E79D01"),

        // Vector 2 — 2-byte message
        Eax("EAX paper App. G #2 — 2-byte message",
            keyHex: "91945D3F4DCBEE0BF45EF52255F095A4",
            nonceHex: "BECAF043B0A23D843194BA972C66DEBD",
            aadHex: "FA3BFD4806EB53FA",
            ptHex: "F7FB",
            ctHex: "19DD",
            tagHex: "5C4C9331049D0BDAB0277408F67967E5"),

        // Vector 3 — 5-byte message
        Eax("EAX paper App. G #3 — 5-byte message",
            keyHex: "01F74AD64077F2E704C0F60ADA3DD523",
            nonceHex: "70C3DB4F0D26368400A10ED05D2BFF5E",
            aadHex: "234A3463C1264AC6",
            ptHex: "1A47CB4933",
            ctHex: "D851D5BAE0",
            tagHex: "3A59F238A23E39199DC9266626C40F80"),

        // Vector 4 — 5-byte message
        Eax("EAX paper App. G #4 — 5-byte message",
            keyHex: "D07CF6CBB7F313BDDE66B727AFD3C5E8",
            nonceHex: "8408DFFF3C1A2B1292DC199E46B7D617",
            aadHex: "33CCE2EABFF5A79D",
            ptHex: "481C9E39B1",
            ctHex: "632A9D131A",
            tagHex: "D4C168A4225D8E1FF755939974A7BEDE"),

        // Vector 5 — 6-byte message
        Eax("EAX paper App. G #5 — 6-byte message",
            keyHex: "35B6D0580005BBC12B0587124557D2C2",
            nonceHex: "FDB6B06676EEDC5C61D74276E1F8E816",
            aadHex: "AEB96EAEBE2970E9",
            ptHex: "40D0C07DA5E4",
            ctHex: "071DFE16C675",
            tagHex: "CB0677E536F73AFE6A14B74EE49844DD"),

        // Vector 6 — 12-byte message
        Eax("EAX paper App. G #6 — 12-byte message",
            keyHex: "BD8E6E11475E60B268784C38C62FEB22",
            nonceHex: "6EAC5C93072D8E8513F750935E46DA1B",
            aadHex: "D4482D1CA78DCE0F",
            ptHex: "4DE3B35C3FC039245BD1FB7D",
            ctHex: "835BB4F15D743E350E728414",
            tagHex: "ABB8644FD6CCB86947C5E10590210A4F"),

        // Vector 7 — 17-byte message
        Eax("EAX paper App. G #7 — 17-byte message",
            keyHex: "7C77D6E813BED5AC98BAA417477A2E7D",
            nonceHex: "1A8C98DCD73D38393B2BF1569DEEFC19",
            aadHex: "65D2017990D62528",
            ptHex: "8B0A79306C9CE7ED99DAE4F87F8DD61636",
            ctHex: "02083E3979DA014812F59F11D52630DA30",
            tagHex: "137327D10649B0AA6E1C181DB617D7F2"),

        // Vector 8 — 18-byte message
        Eax("EAX paper App. G #8 — 18-byte message",
            keyHex: "5FFF20CAFAB119CA2FC73549E20F5B0D",
            nonceHex: "DDE59B97D722156D4D9AFF2BC7559826",
            aadHex: "54B9F04E6A09189A",
            ptHex: "1BDA122BCE8A8DBAF1877D962B8592DD2D56",
            ctHex: "2EC47B2C4954A489AFC7BA4897EDCDAE8CC3",
            tagHex: "3B60450599BD02C96382902AEF7F832A"),

        // Vector 9 — 18-byte message
        Eax("EAX paper App. G #9 — 18-byte message",
            keyHex: "A4A4782BCFFD3EC5E7EF6D8C34A56123",
            nonceHex: "B781FCF2F75FA5A8DE97A9CA48E522EC",
            aadHex: "899A175897561D7E",
            ptHex: "6CF36720872B8513F6EAB1A8A44438D5EF11",
            ctHex: "0DE18FD0FDD91E7AF19F1D8EE8733938B1E8",
            tagHex: "E7F6D2231618102FDB7FE55FF1991700"),

        // Vector 10 — 21-byte message
        Eax("EAX paper App. G #10 — 21-byte message",
            keyHex: "8395FCF1E95BEBD697BD010BC766AAC3",
            nonceHex: "22E7ADD93CFC6393C57EC0B3C17D6B44",
            aadHex: "126735FCC320D25A",
            ptHex: "CA40D7446E545FFAED3BD12A740A659FFBBB3CEAB7",
            ctHex: "CB8920F87A6C75CFF39627B56E3ED197C552D295A7",
            tagHex: "CFC46AFC253B4652B1AF3795B124AB6E"),
    ];

    /// <summary>
    /// Builds an <see cref="AeadKnownAnswer" /> from the EAX paper Appendix G hex fields.
    /// </summary>
    /// <param name="testName">The human-readable scenario label surfaced on failure.</param>
    /// <param name="keyHex">The AES key, hex-encoded.</param>
    /// <param name="nonceHex">The nonce, hex-encoded.</param>
    /// <param name="aadHex">The header / associated data, hex-encoded.</param>
    /// <param name="ptHex">The plaintext, hex-encoded; empty for no plaintext.</param>
    /// <param name="ctHex">The expected ciphertext, hex-encoded; empty for no ciphertext.</param>
    /// <param name="tagHex">The expected authentication tag, hex-encoded.</param>
    /// <returns>An <see cref="AeadKnownAnswer" /> carrying the decoded inputs and detached outputs.</returns>
    private static AeadKnownAnswer Eax(
        string testName,
        string keyHex,
        string nonceHex,
        string aadHex,
        string ptHex,
        string ctHex,
        string tagHex) =>
        new()
        {
            Name = testName,
            Provenance = KatProvenance.ReferenceImplementation("Bellare, Rogaway & Wagner, \"The EAX Mode of Operation\" (FSE 2004), Appendix G"),
            Key = Hex(keyHex),
            Nonce = Hex(nonceHex),
            AssociatedData = aadHex.Length == 0 ? [] : Hex(aadHex),
            Plaintext = ptHex.Length == 0 ? [] : Hex(ptHex),
            Ciphertext = ctHex.Length == 0 ? [] : Hex(ctHex),
            Tag = Hex(tagHex),
            Layout = AeadKatOutputLayout.CiphertextThenTag,
        };

    /// <summary>
    /// Yields the EAX paper Appendix G known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> EaxKatVectors()
    {
        foreach (AeadKnownAnswer kat in KnownAnswers)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that EAX-mode encryption reproduces each EAX paper Appendix G ciphertext and tag.
    /// </summary>
    /// <param name="vector">The EAX known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(EaxKatVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WithEaxPaperVector_ShouldProduceExpectedCiphertextAndTag(AeadKnownAnswer vector)
        => AssertKatEncrypt(vector);

    /// <summary>
    /// Verifies that EAX-mode decryption recovers the original plaintext for each EAX paper Appendix G vector.
    /// </summary>
    /// <param name="vector">The EAX known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(EaxKatVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WithEaxPaperVector_ShouldRecoverOriginalPlaintext(AeadKnownAnswer vector)
        => AssertKatDecrypt(vector);
}
