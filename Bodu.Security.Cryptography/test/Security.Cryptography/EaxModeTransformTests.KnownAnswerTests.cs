// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

// Known-answer vectors — Bellare, Rogaway and Wagner, "The EAX Mode of Operation" (FSE 2004),
// Appendix G. All vectors use AES-128 with a 16-byte nonce, 8-byte header (AAD), and
// ciphertext encoded as C || T (ciphertext followed by the 16-byte authentication tag).
//
// Source: https://web.cs.ucdavis.edu/~rogaway/papers/eax.pdf
public sealed partial class EaxModeTransformTests
{
    private static IEnumerable<object[]> EaxKatVectors()
    {
        // Vector 1 — empty message
        yield return new object[]
        {
            "EAX paper App. G #1 — empty message",
            Convert.FromHexString("233952DEE4D5ED5F9B9C6D6FF80FF478"),   // key
            Convert.FromHexString("62EC67F9C3A4A407FCB2A8C49031A8B3"),   // nonce
            Convert.FromHexString("6BFB914FD07EAE6B"),                   // header
            Array.Empty<byte>(),                                          // plaintext
            Array.Empty<byte>(),                                          // expected ciphertext
            Convert.FromHexString("E037830E8389F27B025A2D6527E79D01"),   // expected tag
        };

        // Vector 2 — 2-byte message
        yield return new object[]
        {
            "EAX paper App. G #2 — 2-byte message",
            Convert.FromHexString("91945D3F4DCBEE0BF45EF52255F095A4"),
            Convert.FromHexString("BECAF043B0A23D843194BA972C66DEBD"),
            Convert.FromHexString("FA3BFD4806EB53FA"),
            Convert.FromHexString("F7FB"),
            Convert.FromHexString("19DD"),
            Convert.FromHexString("5C4C9331049D0BDAB0277408F67967E5"),
        };

        // Vector 3 — 5-byte message
        yield return new object[]
        {
            "EAX paper App. G #3 — 5-byte message",
            Convert.FromHexString("01F74AD64077F2E704C0F60ADA3DD523"),
            Convert.FromHexString("70C3DB4F0D26368400A10ED05D2BFF5E"),
            Convert.FromHexString("234A3463C1264AC6"),
            Convert.FromHexString("1A47CB4933"),
            Convert.FromHexString("D851D5BAE0"),
            Convert.FromHexString("3A59F238A23E39199DC9266626C40F80"),
        };

        // Vector 4 — 5-byte message
        yield return new object[]
        {
            "EAX paper App. G #4 — 5-byte message",
            Convert.FromHexString("D07CF6CBB7F313BDDE66B727AFD3C5E8"),
            Convert.FromHexString("8408DFFF3C1A2B1292DC199E46B7D617"),
            Convert.FromHexString("33CCE2EABFF5A79D"),
            Convert.FromHexString("481C9E39B1"),
            Convert.FromHexString("632A9D131A"),
            Convert.FromHexString("D4C168A4225D8E1FF755939974A7BEDE"),
        };

        // Vector 5 — 6-byte message
        yield return new object[]
        {
            "EAX paper App. G #5 — 6-byte message",
            Convert.FromHexString("35B6D0580005BBC12B0587124557D2C2"),
            Convert.FromHexString("FDB6B06676EEDC5C61D74276E1F8E816"),
            Convert.FromHexString("AEB96EAEBE2970E9"),
            Convert.FromHexString("40D0C07DA5E4"),
            Convert.FromHexString("071DFE16C675"),
            Convert.FromHexString("CB0677E536F73AFE6A14B74EE49844DD"),
        };

        // Vector 6 — 12-byte message
        yield return new object[]
        {
            "EAX paper App. G #6 — 12-byte message",
            Convert.FromHexString("BD8E6E11475E60B268784C38C62FEB22"),
            Convert.FromHexString("6EAC5C93072D8E8513F750935E46DA1B"),
            Convert.FromHexString("D4482D1CA78DCE0F"),
            Convert.FromHexString("4DE3B35C3FC039245BD1FB7D"),
            Convert.FromHexString("835BB4F15D743E350E728414"),
            Convert.FromHexString("ABB8644FD6CCB86947C5E10590210A4F"),
        };

        // Vector 7 — 17-byte message
        yield return new object[]
        {
            "EAX paper App. G #7 — 17-byte message",
            Convert.FromHexString("7C77D6E813BED5AC98BAA417477A2E7D"),
            Convert.FromHexString("1A8C98DCD73D38393B2BF1569DEEFC19"),
            Convert.FromHexString("65D2017990D62528"),
            Convert.FromHexString("8B0A79306C9CE7ED99DAE4F87F8DD61636"),
            Convert.FromHexString("02083E3979DA014812F59F11D52630DA30"),
            Convert.FromHexString("137327D10649B0AA6E1C181DB617D7F2"),
        };

        // Vector 8 — 18-byte message
        yield return new object[]
        {
            "EAX paper App. G #8 — 18-byte message",
            Convert.FromHexString("5FFF20CAFAB119CA2FC73549E20F5B0D"),
            Convert.FromHexString("DDE59B97D722156D4D9AFF2BC7559826"),
            Convert.FromHexString("54B9F04E6A09189A"),
            Convert.FromHexString("1BDA122BCE8A8DBAF1877D962B8592DD2D56"),
            Convert.FromHexString("2EC47B2C4954A489AFC7BA4897EDCDAE8CC3"),
            Convert.FromHexString("3B60450599BD02C96382902AEF7F832A"),
        };

        // Vector 9 — 18-byte message
        yield return new object[]
        {
            "EAX paper App. G #9 — 18-byte message",
            Convert.FromHexString("A4A4782BCFFD3EC5E7EF6D8C34A56123"),
            Convert.FromHexString("B781FCF2F75FA5A8DE97A9CA48E522EC"),
            Convert.FromHexString("899A175897561D7E"),
            Convert.FromHexString("6CF36720872B8513F6EAB1A8A44438D5EF11"),
            Convert.FromHexString("0DE18FD0FDD91E7AF19F1D8EE8733938B1E8"),
            Convert.FromHexString("E7F6D2231618102FDB7FE55FF1991700"),
        };

        // Vector 10 — 21-byte message
        yield return new object[]
        {
            "EAX paper App. G #10 — 21-byte message",
            Convert.FromHexString("8395FCF1E95BEBD697BD010BC766AAC3"),
            Convert.FromHexString("22E7ADD93CFC6393C57EC0B3C17D6B44"),
            Convert.FromHexString("126735FCC320D25A"),
            Convert.FromHexString("CA40D7446E545FFAED3BD12A740A659FFBBB3CEAB7"),
            Convert.FromHexString("CB8920F87A6C75CFF39627B56E3ED197C552D295A7"),
            Convert.FromHexString("CFC46AFC253B4652B1AF3795B124AB6E"),
        };
    }

    [TestMethod]

    [DynamicData(nameof(EaxKatVectors))]
    public void Encrypt_WithEaxPaperVector_ShouldProduceExpectedCiphertextAndTag(
        string description, byte[] key, byte[] iv, byte[] aad,
        byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
        => AssertKatEncrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);

    [TestMethod]

    [DynamicData(nameof(EaxKatVectors))]
    public void Decrypt_WithEaxPaperVector_ShouldRecoverOriginalPlaintext(
        string description, byte[] key, byte[] iv, byte[] aad,
        byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
        => AssertKatDecrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);
}
