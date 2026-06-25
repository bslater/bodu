// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the behavioural contract of the HPKE surface (<see cref="Hpke" />, <see cref="HpkeSender" />,
/// <see cref="HpkeReceiver" />, and <see cref="HpkeSuite" />): round-trip correctness across every suite and mode,
/// authentication failure handling, sequence ordering, secret export agreement, export-only restrictions, argument
/// validation, and disposal.
/// </summary>
[TestClass]
public sealed partial class HpkeTests
{
    /// <summary>A representative application info value.</summary>
    private static readonly byte[] Info = Encoding.ASCII.GetBytes("hpke unit test info");

    /// <summary>A representative associated-data value.</summary>
    private static readonly byte[] Aad = Encoding.ASCII.GetBytes("aad");

    /// <summary>A representative plaintext value.</summary>
    private static readonly byte[] Plaintext = Encoding.ASCII.GetBytes("the quick brown fox");

    /// <summary>A representative pre-shared key (at least 32 bytes per RFC 9180 guidance).</summary>
    private static readonly byte[] Psk = Convert.FromHexString("0247fd33b913760fa1fa51e1892d9f307fbe65eb171e8132c2af18555a738b82");

    /// <summary>A representative pre-shared key identifier.</summary>
    private static readonly byte[] PskId = Encoding.ASCII.GetBytes("psk-id");

    /// <summary>
    /// Verifies that a single-shot base-mode message sealed by <see cref="Hpke.Seal" /> round-trips through
    /// <see cref="Hpke.Open" /> back to the original plaintext.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Seal_WhenBaseModeSingleShot_ShouldRoundTripThroughOpen()
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();
        HpkeSuite suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;

        var (encapsulation, ciphertext) = Hpke.Seal(suite, recipient.ExportPublicKey(), Info, Aad, Plaintext);
        byte[] recovered = Hpke.Open(suite, recipient, encapsulation, Info, Aad, ciphertext);

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that a message sealed by a sender round-trips through the matching receiver for every supported AEAD
    /// and establishment mode.
    /// </summary>
    /// <param name="aead">The AEAD function under test.</param>
    /// <param name="mode">The establishment mode under test.</param>
    [TestMethod]
    [DataRow(HpkeAead.Aes128Gcm, HpkeMode.Base)]
    [DataRow(HpkeAead.Aes128Gcm, HpkeMode.Psk)]
    [DataRow(HpkeAead.Aes128Gcm, HpkeMode.Auth)]
    [DataRow(HpkeAead.Aes128Gcm, HpkeMode.AuthPsk)]
    [DataRow(HpkeAead.Aes256Gcm, HpkeMode.Base)]
    [DataRow(HpkeAead.Aes256Gcm, HpkeMode.Psk)]
    [DataRow(HpkeAead.Aes256Gcm, HpkeMode.Auth)]
    [DataRow(HpkeAead.Aes256Gcm, HpkeMode.AuthPsk)]
    [DataRow(HpkeAead.ChaCha20Poly1305, HpkeMode.Base)]
    [DataRow(HpkeAead.ChaCha20Poly1305, HpkeMode.Psk)]
    [DataRow(HpkeAead.ChaCha20Poly1305, HpkeMode.Auth)]
    [DataRow(HpkeAead.ChaCha20Poly1305, HpkeMode.AuthPsk)]
    public void SealAndOpen_WhenGivenSuiteAndMode_ShouldRoundTrip(HpkeAead aead, HpkeMode mode)
    {
        var suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, aead);

        var (sender, receiver) = CreatePair(suite, mode);
        try
        {
            byte[] ciphertext = sender.Seal(Aad, Plaintext);
            byte[] recovered = receiver.Open(Aad, ciphertext);

            CollectionAssert.AreEqual(Plaintext, recovered);
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that several messages sealed in sequence open back, in the same order, to their original plaintexts.
    /// </summary>
    [TestMethod]
    public void SealAndOpen_WhenMultipleMessages_ShouldRoundTripInSequence()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[][] messages =
            [
                Encoding.ASCII.GetBytes("message zero"),
                Encoding.ASCII.GetBytes("message one"),
                Encoding.ASCII.GetBytes("message two"),
            ];

            byte[][] ciphertexts = messages.Select(m => sender.Seal(Aad, m)).ToArray();

            for (int i = 0; i < messages.Length; i++)
                CollectionAssert.AreEqual(messages[i], receiver.Open(Aad, ciphertexts[i]));
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that opening messages out of the order they were sealed fails authentication, because each sequence
    /// number derives a distinct nonce.
    /// </summary>
    [TestMethod]
    public void Open_WhenMessagesOpenedOutOfOrder_ShouldThrowCryptographicException()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[] first = sender.Seal(Aad, Plaintext);
            byte[] second = sender.Seal(Aad, Plaintext);

            _ = second;

            // The receiver is at sequence 0; opening the second message (sealed at sequence 1) uses the wrong nonce.
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _ = receiver.Open(Aad, second);
            });

            _ = first;
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a tampered ciphertext fails authentication and throws <see cref="CryptographicException" /> for
    /// each AEAD.
    /// </summary>
    /// <param name="aead">The AEAD function under test.</param>
    [TestMethod]
    [DataRow(HpkeAead.Aes128Gcm)]
    [DataRow(HpkeAead.Aes256Gcm)]
    [DataRow(HpkeAead.ChaCha20Poly1305)]
    public void Open_WhenCiphertextTampered_ShouldThrowCryptographicException(HpkeAead aead)
    {
        var suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, aead);

        var (sender, receiver) = CreatePair(suite, HpkeMode.Base);
        try
        {
            byte[] ciphertext = sender.Seal(Aad, Plaintext);
            ciphertext[0] ^= 0xFF;

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _ = receiver.Open(Aad, ciphertext);
            });
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that opening with associated data that differs from what was sealed fails authentication.
    /// </summary>
    [TestMethod]
    public void Open_WhenAssociatedDataDiffers_ShouldThrowCryptographicException()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[] ciphertext = sender.Seal(Aad, Plaintext);

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _ = receiver.Open(Encoding.ASCII.GetBytes("different aad"), ciphertext);
            });
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the sender and receiver derive identical exported secrets for the same context and length.
    /// </summary>
    [TestMethod]
    public void Export_WhenSenderAndReceiverAgree_ShouldProduceIdenticalSecret()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[] context = Encoding.ASCII.GetBytes("exporter context");

            byte[] senderSecret = sender.Export(context, 32);
            byte[] receiverSecret = receiver.Export(context, 32);

            CollectionAssert.AreEqual(senderSecret, receiverSecret);
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that <see cref="HpkeSender.Export" /> accepts lengths up to 255·Nh inclusive and rejects negative or
    /// larger lengths with <see cref="ArgumentOutOfRangeException" />, enforcing the RFC 9180 §5.3 export bound at the
    /// HPKE layer rather than relying on a lower KDF stage.
    /// </summary>
    [TestMethod]
    public void Export_WhenLengthOutsideBounds_ShouldThrowArgumentOutOfRangeException()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[] context = Encoding.ASCII.GetBytes("exporter context");
            const int maxLength = 255 * 32; // Nh = 32 for HKDF-SHA256.

            Assert.AreEqual(0, sender.Export(context, 0).Length);
            Assert.AreEqual(maxLength, sender.Export(context, maxLength).Length);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = sender.Export(context, maxLength + 1); });
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = sender.Export(context, -1); });
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a sender at the sequence-number limit refuses to seal and that the final usable sequence value
    /// still succeeds exactly once before the limit is reached (RFC 9180 §5.2 <c>MessageLimitReached</c>).
    /// </summary>
    [TestMethod]
    public void Seal_WhenSequenceLimitReached_ShouldThrowInvalidOperationException()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            SetSequence(sender, ulong.MaxValue);
            Assert.ThrowsExactly<InvalidOperationException>(() => { _ = sender.Seal(Aad, Plaintext); });

            // The penultimate value is still usable; the increment that follows reaches the limit.
            SetSequence(sender, ulong.MaxValue - 1);
            byte[] ciphertext = sender.Seal(Aad, Plaintext);
            Assert.IsNotNull(ciphertext);
            Assert.ThrowsExactly<InvalidOperationException>(() => { _ = sender.Seal(Aad, Plaintext); });
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a receiver at the sequence-number limit throws <see cref="InvalidOperationException" /> rather
    /// than attempting authentication, proving the limit is checked before any AEAD work (RFC 9180 §5.2).
    /// </summary>
    [TestMethod]
    public void Open_WhenSequenceLimitReached_ShouldThrowInvalidOperationExceptionBeforeAuthenticating()
    {
        var (sender, receiver) = CreatePair(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, HpkeMode.Base);
        try
        {
            byte[] ciphertext = sender.Seal(Aad, Plaintext);

            SetSequence(receiver, ulong.MaxValue);

            // A limit reached before AEAD surfaces as InvalidOperationException; a post-AEAD check would instead fail
            // authentication (the nonce no longer matches) and throw CryptographicException.
            Assert.ThrowsExactly<InvalidOperationException>(() => { _ = receiver.Open(Aad, ciphertext); });
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that an export-only suite supports secret export between sender and receiver but rejects sealing.
    /// </summary>
    [TestMethod]
    public void Seal_WhenSuiteIsExportOnly_ShouldThrowNotSupportedButExportAgrees()
    {
        var suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ExportOnly);

        var (sender, receiver) = CreatePair(suite, HpkeMode.Base);
        try
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                _ = sender.Seal(Aad, Plaintext);
            });

            byte[] context = Encoding.ASCII.GetBytes("exporter context");
            CollectionAssert.AreEqual(sender.Export(context, 32), receiver.Export(context, 32));
        }
        finally
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }

    /// <summary>
    /// Verifies that an export-only suite reports zero AEAD element sizes.
    /// </summary>
    [TestMethod]
    public void HpkeSuite_WhenExportOnly_ShouldReportZeroAeadSizes()
    {
        var suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ExportOnly);

        Assert.IsTrue(suite.IsExportOnly);
        Assert.AreEqual(0, suite.AeadKeySizeInBytes);
        Assert.AreEqual(0, suite.AeadNonceSizeInBytes);
        Assert.AreEqual(0, suite.AeadTagSizeInBytes);
    }

    /// <summary>
    /// Verifies that each suite reports the AEAD key, nonce, and tag sizes mandated by RFC 9180.
    /// </summary>
    /// <param name="aead">The AEAD function under test.</param>
    /// <param name="expectedKeySize">The expected key size in bytes.</param>
    /// <param name="expectedNonceSize">The expected nonce size in bytes.</param>
    /// <param name="expectedTagSize">The expected tag size in bytes.</param>
    [TestMethod]
    [DataRow(HpkeAead.Aes128Gcm, 16, 12, 16)]
    [DataRow(HpkeAead.Aes256Gcm, 32, 12, 16)]
    [DataRow(HpkeAead.ChaCha20Poly1305, 32, 12, 16)]
    public void HpkeSuite_WhenConstructed_ShouldReportAeadSizes(HpkeAead aead, int expectedKeySize, int expectedNonceSize, int expectedTagSize)
    {
        var suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, aead);

        Assert.AreEqual(expectedKeySize, suite.AeadKeySizeInBytes);
        Assert.AreEqual(expectedNonceSize, suite.AeadNonceSizeInBytes);
        Assert.AreEqual(expectedTagSize, suite.AeadTagSizeInBytes);
        Assert.AreEqual(32, suite.SharedSecretSizeInBytes);
        Assert.AreEqual(32, suite.EncapsulationSizeInBytes);
    }

    /// <summary>
    /// Verifies that constructing a suite with an unsupported AEAD identifier throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void HpkeSuite_WhenAeadUnsupported_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, (HpkeAead)0x1234);
        });

        Assert.AreEqual("aead", ex.ParamName);
    }

    /// <summary>
    /// Verifies that setting up a sender with a recipient public key of the wrong length throws
    /// <see cref="ArgumentException" /> naming the offending parameter.
    /// </summary>
    [TestMethod]
    public void SetupBase_WhenRecipientPublicKeyWrongLength_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = HpkeSender.SetupBase(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, new byte[31], Info, out _);
        });

        Assert.AreEqual("recipientPublicKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening with an encapsulated key of the wrong length throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void SetupBase_WhenEncapsulationWrongLength_ShouldThrowArgumentException()
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = HpkeReceiver.SetupBase(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, recipient, new byte[16], Info);
        });

        Assert.AreEqual("encapsulation", ex.ParamName);
    }

    /// <summary>
    /// Verifies that PSK setup with only one of the pre-shared key and its identifier supplied throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void SetupPsk_WhenPskInputsInconsistent_ShouldThrowCryptographicException()
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = HpkeSender.SetupPsk(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, recipient.ExportPublicKey(), Info, Psk, ReadOnlySpan<byte>.Empty, out _);
        });
    }

    /// <summary>
    /// Verifies that PSK setup without any pre-shared key throws <see cref="CryptographicException" />, because PSK mode
    /// requires one.
    /// </summary>
    [TestMethod]
    public void SetupPsk_WhenNoPskSupplied_ShouldThrowCryptographicException()
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = HpkeSender.SetupPsk(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, recipient.ExportPublicKey(), Info, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, out _);
        });
    }

    /// <summary>
    /// Verifies that sealing through a disposed sender throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Seal_WhenSenderDisposed_ShouldThrowObjectDisposedException()
    {
        HpkeSender sender = HpkeSender.SetupBase(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, NewRecipientPublicKey(), Info, out _);
        sender.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sender.Seal(Aad, Plaintext);
        });
    }

    /// <summary>
    /// Verifies that opening with a recipient key that holds no private key throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void SetupBase_WhenRecipientHasNoPrivateKey_ShouldThrowCryptographicException()
    {
        using var sender = X25519.Create();
        sender.GenerateKey();
        var (encapsulation, _) = Hpke.Seal(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, sender.ExportPublicKey(), Info, Aad, Plaintext);

        using var publicOnly = X25519.Create();
        publicOnly.ImportPublicKey(sender.ExportPublicKey());

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = HpkeReceiver.SetupBase(HpkeSuite.X25519_HkdfSha256_Aes128Gcm, publicOnly, encapsulation, Info);
        });
    }

    /// <summary>
    /// Creates a fresh random X25519 recipient public key.
    /// </summary>
    /// <returns>A 32-byte X25519 public key.</returns>
    private static byte[] NewRecipientPublicKey()
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();
        return recipient.ExportPublicKey();
    }

    /// <summary>
    /// Creates a matched sender and receiver for the given suite and mode, generating fresh recipient and (for auth
    /// modes) sender key pairs.
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="mode">The establishment mode.</param>
    /// <returns>A matched sender and receiver sharing a freshly encapsulated secret.</returns>
    private static (HpkeSender Sender, HpkeReceiver Receiver) CreatePair(HpkeSuite suite, HpkeMode mode)
    {
        using var recipient = X25519.Create();
        recipient.GenerateKey();
        byte[] recipientPublicKey = recipient.ExportPublicKey();

        using var senderKey = X25519.Create();
        senderKey.GenerateKey();
        byte[] senderPublicKey = senderKey.ExportPublicKey();

        byte[] encapsulation;
        HpkeSender sender = mode switch
        {
            HpkeMode.Base => HpkeSender.SetupBase(suite, recipientPublicKey, Info, out encapsulation),
            HpkeMode.Psk => HpkeSender.SetupPsk(suite, recipientPublicKey, Info, Psk, PskId, out encapsulation),
            HpkeMode.Auth => HpkeSender.SetupAuth(suite, recipientPublicKey, Info, senderKey, out encapsulation),
            _ => HpkeSender.SetupAuthPsk(suite, recipientPublicKey, Info, senderKey, Psk, PskId, out encapsulation),
        };

        HpkeReceiver receiver = mode switch
        {
            HpkeMode.Base => HpkeReceiver.SetupBase(suite, recipient, encapsulation, Info),
            HpkeMode.Psk => HpkeReceiver.SetupPsk(suite, recipient, encapsulation, Info, Psk, PskId),
            HpkeMode.Auth => HpkeReceiver.SetupAuth(suite, recipient, encapsulation, Info, senderPublicKey),
            _ => HpkeReceiver.SetupAuthPsk(suite, recipient, encapsulation, Info, senderPublicKey, Psk, PskId),
        };

        return (sender, receiver);
    }

    /// <summary>
    /// Forces the sequence counter of a sender's or receiver's underlying <c>HpkeContext</c> to a chosen value so the
    /// message-limit boundary can be exercised without sealing 2⁶⁴ messages.
    /// </summary>
    /// <param name="endpoint">The <see cref="HpkeSender" /> or <see cref="HpkeReceiver" /> whose counter is set.</param>
    /// <param name="value">The sequence value to assign.</param>
    private static void SetSequence(object endpoint, ulong value)
    {
        FieldInfo contextField = endpoint.GetType().GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object context = contextField.GetValue(endpoint)!;
        FieldInfo seqField = context.GetType().GetField("_seq", BindingFlags.Instance | BindingFlags.NonPublic)!;
        seqField.SetValue(context, value);
    }
}
