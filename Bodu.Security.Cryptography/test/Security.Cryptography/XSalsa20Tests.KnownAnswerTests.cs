// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Locks the <see cref="XSalsa20" /> extended-nonce stream cipher and its HSalsa20 subkey-derivation core against the
/// published NaCl / libsodium known-answer test vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmContractTests{TCipher}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed class XSalsa20Tests
    : SymmetricStreamAlgorithmContractTests<XSalsa20>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeBits => 256;

    /// <inheritdoc />
    protected override int ExpectedNonceSizeBits => 192;

    /// <summary>
    /// Verifies that the internal HSalsa20 subkey-derivation core reproduces the canonical NaCl
    /// <c>crypto_core_hsalsa20</c> reference vector.
    /// </summary>
    [TestMethod]
    public void HSalsa20_WhenGivenNaClVector_ShouldDeriveExpectedSubkey()
    {
        byte[] key = Convert.FromHexString("1b27556473e985d462cd51197a9a46c76009549eac6474f206c4ee0844f68389");
        byte[] nonce = Convert.FromHexString("69696ee955b62b73cd62bda875fc73d6");
        byte[] expected = Convert.FromHexString("dc908dda0b9344a953629b733820778880f3ceb421bb61b91cbd4c3e66256ce4");

        byte[] subkey = new byte[32];
        Salsa20StreamCipher.HSalsa20(key, nonce, subkey);

        CollectionAssert.AreEqual(expected, subkey, "HSalsa20 subkey mismatch for the NaCl core vector.");
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20" /> reproduces the NaCl-derived keystream vector (the ASCII
    /// <c>"this is 32-byte key for xsalsa20"</c> / <c>"24-byte nonce for xsalsa"</c> case), recovered as the ciphertext
    /// of an all-zero plaintext.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenGivenNaClKeystreamVector_ShouldMatchExpected()
    {
        byte[] key = Encoding.ASCII.GetBytes("this is 32-byte key for xsalsa20");
        byte[] nonce = Encoding.ASCII.GetBytes("24-byte nonce for xsalsa");
        byte[] expected = Convert.FromHexString(
            "4848297feb1fb52fb66d81609bd547fabcbe7026edc8b5e5e449d088bfa69c08" +
            "8f5d8da1d791267c2c195a7f8cae9c4b4050d08ce6d3a151ec265f3a58e47648");

        using var cipher = new XSalsa20();
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        byte[] keystream = encryptor.TransformFinalBlock(new byte[expected.Length], 0, expected.Length);

        CollectionAssert.AreEqual(expected, keystream, "XSalsa20 keystream mismatch for the NaCl vector.");
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20" /> equals running <see cref="Salsa20" /> under the HSalsa20-derived subkey and
    /// the trailing 64-bit nonce, confirming the extended-nonce construction is wired correctly.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenComparedToDerivedSalsa20_ShouldProduceSameKeystream()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(24);
        byte[] zeros = new byte[256];

        byte[] xActual;
        using (var x = new XSalsa20())
        using (ICryptoTransform e = x.CreateEncryptor(key, nonce))
            xActual = e.TransformFinalBlock(zeros, 0, zeros.Length);

        byte[] subkey = new byte[32];
        Salsa20StreamCipher.HSalsa20(key, nonce.AsSpan(0, 16), subkey);
        byte[] salsaNonce = nonce[16..24];

        byte[] sActual;
        using (var s = new Salsa20())
        using (ICryptoTransform e = s.CreateEncryptor(subkey, salsaNonce))
            sActual = e.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(sActual, xActual,
            "XSalsa20 keystream must equal Salsa20 under the HSalsa20-derived subkey.");
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20.InitialCounter" /> is captured when the transform is created, so mutating it on
    /// the algorithm afterwards does not change an already-created transform's keystream.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInitialCounterChangedAfterCreation_ShouldUseCapturedCounter()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[24];
        byte[] plaintext = new byte[128];

        using var cipher = new XSalsa20 { InitialCounter = 5 };
        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);
        cipher.InitialCounter = 9;
        byte[] actual = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        using var reference = new XSalsa20 { InitialCounter = 5 };
        using ICryptoTransform referenceTransform = reference.CreateEncryptor(key, nonce);
        byte[] expected = referenceTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual,
            "The transform must use the counter captured at creation, not the algorithm's later value.");
    }
}
