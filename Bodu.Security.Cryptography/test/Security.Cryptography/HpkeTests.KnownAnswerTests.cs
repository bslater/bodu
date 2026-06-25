// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks the HPKE receiver path against the RFC 9180 Appendix A known-answer vectors embedded in the test assembly:
/// for each suite and mode, every published sealed message must open to the expected plaintext and every published
/// export must reproduce the expected secret.
/// </summary>
public sealed partial class HpkeTests
{
    /// <summary>Resource name of the embedded RFC 9180 HPKE known-answer vectors.</summary>
    private const string VectorResourceName = "Bodu.Security.Cryptography.Hpke.Rfc9180Vectors.json";

    /// <summary>
    /// Yields every embedded HPKE known-answer vector as a <see cref="DynamicDataAttribute" />-compatible row.
    /// </summary>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> KnownAnswers() =>
        ReadKnownAnswers().Select(v => new object[] { v });

    /// <summary>
    /// Verifies that an <see cref="HpkeReceiver" /> set up from a published vector opens every sealed message, in
    /// sequence, to the expected plaintext — exercising decapsulation, the key schedule, the per-message nonce, and the
    /// AEAD against the RFC 9180 reference outputs.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(KnownAnswers),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenGivenRfc9180Vector_ShouldRecoverPublishedPlaintext(HpkeKnownAnswer vector)
    {
        using var recipientKey = new X25519();
        recipientKey.ImportPrivateKey(vector.RecipientPrivateKey);

        using HpkeReceiver receiver = CreateReceiver(vector, recipientKey);

        foreach (HpkeKnownAnswer.Encryption encryption in vector.Encryptions)
        {
            byte[] plaintext = receiver.Open(encryption.AssociatedData, encryption.Ciphertext);
            CollectionAssert.AreEqual(encryption.Plaintext, plaintext);
        }
    }

    /// <summary>
    /// Verifies that an <see cref="HpkeReceiver" /> set up from a published vector reproduces every exported secret for
    /// the published exporter contexts and lengths.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(KnownAnswers),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Export_WhenGivenRfc9180Vector_ShouldReproducePublishedSecret(HpkeKnownAnswer vector)
    {
        using var recipientKey = new X25519();
        recipientKey.ImportPrivateKey(vector.RecipientPrivateKey);

        using HpkeReceiver receiver = CreateReceiver(vector, recipientKey);

        foreach (HpkeKnownAnswer.Export export in vector.Exports)
        {
            byte[] exported = receiver.Export(export.Context, export.Length);
            CollectionAssert.AreEqual(export.Value, exported);
        }
    }

    /// <summary>
    /// Builds the receiver for a vector by dispatching on its establishment mode.
    /// </summary>
    /// <param name="vector">The known-answer vector.</param>
    /// <param name="recipientKey">The recipient key initialized with the vector's private key.</param>
    /// <returns>A receiver context ready to open the vector's messages.</returns>
    private static HpkeReceiver CreateReceiver(HpkeKnownAnswer vector, X25519 recipientKey) =>
        vector.Mode switch
        {
            HpkeMode.Base => HpkeReceiver.SetupBase(vector.Suite, recipientKey, vector.Encapsulation, vector.Info),
            HpkeMode.Psk => HpkeReceiver.SetupPsk(vector.Suite, recipientKey, vector.Encapsulation, vector.Info, vector.Psk, vector.PskId),
            HpkeMode.Auth => HpkeReceiver.SetupAuth(vector.Suite, recipientKey, vector.Encapsulation, vector.Info, vector.SenderPublicKey),
            _ => HpkeReceiver.SetupAuthPsk(vector.Suite, recipientKey, vector.Encapsulation, vector.Info, vector.SenderPublicKey, vector.Psk, vector.PskId),
        };

    /// <summary>
    /// Reads all embedded HPKE known-answer vectors.
    /// </summary>
    /// <returns>The parsed vectors in file order.</returns>
    /// <exception cref="InvalidOperationException">The embedded vector resource cannot be located.</exception>
    private static List<HpkeKnownAnswer> ReadKnownAnswers()
    {
        using Stream stream = typeof(HpkeTests).Assembly.GetManifestResourceStream(VectorResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{VectorResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        return HpkeKnownAnswer.Read(stream).ToList();
    }
}
