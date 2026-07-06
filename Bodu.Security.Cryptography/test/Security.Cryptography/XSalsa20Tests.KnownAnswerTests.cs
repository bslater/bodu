// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="XSalsa20" /> extended-nonce stream cipher and its HSalsa20 subkey-derivation core against the
/// published NaCl / libsodium known-answer test vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed partial class XSalsa20Tests
    : SymmetricStreamAlgorithmTests<XSalsa20Tests, XSalsa20>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 256,
            NonceSizeBits = 192,
            LegalKeySizesBits = [256],
        };

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

    /// <summary>Resource name of the embedded Go (NaCl) XSalsa20 vector file.</summary>
    private const string XSalsa20GoResourceName = "Bodu.Security.Cryptography.Salsa20.go-test-vectors.txt";

    /// <summary>
    /// Yields the golang.org/x/crypto XSalsa20 encryption vectors, loaded from the embedded Go test source, as
    /// <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> XSalsa20KatData()
    {
        using Stream stream = typeof(XSalsa20Tests).Assembly.GetManifestResourceStream(XSalsa20GoResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{XSalsa20GoResourceName}' is missing.");

        foreach (StreamCipherKnownAnswer vector in XSalsa20GoVectorReader.Read(stream))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20" /> encrypts each golang.org/x/crypto NaCl XSalsa20 vector — spanning the
    /// short-message and 64-byte-keystream cases — to its published ciphertext.
    /// </summary>
    /// <param name="vector">The XSalsa20 encryption vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(XSalsa20KatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateEncryptor_WhenGivenNaClKeystreamVector_ShouldMatchExpected(StreamCipherKnownAnswer vector)
    {
        using var cipher = new XSalsa20();
        using ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Nonce);
        byte[] actual = encryptor.TransformFinalBlock(vector.Plaintext, 0, vector.Plaintext.Length);

        CollectionAssert.AreEqual(vector.Ciphertext, actual, $"XSalsa20 ciphertext mismatch for {vector.Name}.");
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
