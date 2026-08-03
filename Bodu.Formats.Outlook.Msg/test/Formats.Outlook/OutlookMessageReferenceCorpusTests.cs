// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageReferenceCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the reader against the real-world reference corpus: every valid fixture must open and match the
/// expectations an independent implementation generated, and every invalid fixture must be rejected.
/// </summary>
/// <remarks>
/// Provenance and the adjudication notes live in <c>Fixtures/Reference/NOTICE.md</c>. Valid fixtures are asserted
/// under <see cref="Bodu.IO.Compound.CompoundValidationLevel.Compatible" /> only — tolerating real-world writers is
/// that level's contract. Subjects the oracle recorded with U+FFFD replacement characters come from files whose
/// declared code page does not match their string bytes; those are asserted as present rather than byte-exact,
/// because replacement policies legitimately differ between decoders.
/// </remarks>
[TestClass]
public class OutlookMessageReferenceCorpusTests
{
    /// <summary>
    /// Gets the manifest rows classified as valid.
    /// </summary>
    /// <value>One row per file under <c>Fixtures/Reference/valid/</c>.</value>
    public static IEnumerable<object[]> ValidFixtures =>
        MsgReferenceFixtures.Manifest.Fixtures
            .Where(static fixture => fixture.Expected == "valid")
            .Select(static fixture => new object[] { fixture });

    /// <summary>
    /// Gets the manifest rows classified as invalid.
    /// </summary>
    /// <value>One row per file under <c>Fixtures/Reference/invalid/</c>.</value>
    public static IEnumerable<object[]> InvalidFixtures =>
        MsgReferenceFixtures.Manifest.Fixtures
            .Where(static fixture => fixture.Expected == "invalid")
            .Select(static fixture => new object[] { fixture });

    /// <summary>
    /// Verifies that a valid corpus fixture hashes to its recorded SHA-256, opens under compatible validation, and
    /// matches the independently generated expectations.
    /// </summary>
    /// <param name="fixture">The manifest row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(ValidFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void OpenRead_WhenValidReferenceFixture_ShouldMatchManifest(MsgReferenceFixture fixture)
    {
        using MemoryStream stream = MsgReferenceFixtures.OpenStream("valid", fixture.File);
        Assert.AreEqual(fixture.Sha256, Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant());

        using var message = OutlookMessage.OpenRead(stream, leaveOpen: true);

        Assert.AreEqual(fixture.RecipientCount, message.Recipients.Count);
        Assert.AreEqual(fixture.AttachmentCount, message.Attachments.Count);
        Assert.AreEqual(fixture.HasBodyText, message.BodyText is not null);
        Assert.AreEqual(fixture.HasBodyHtml, message.BodyHtml is not null);
        Assert.AreEqual(fixture.HasBodyRtf, message.BodyRtf is not null);

        AssertOracleString(fixture.Subject, message.Subject);
        AssertOracleString(fixture.SenderName, message.SenderName);
    }

    /// <summary>
    /// Verifies that an invalid corpus fixture hashes to its recorded SHA-256 and is rejected with
    /// <see cref="OutlookMsgFormatException" />.
    /// </summary>
    /// <param name="fixture">The manifest row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(InvalidFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void OpenRead_WhenInvalidReferenceFixture_ShouldThrowOutlookMsgFormatException(MsgReferenceFixture fixture)
    {
        using MemoryStream stream = MsgReferenceFixtures.OpenStream("invalid", fixture.File);
        Assert.AreEqual(fixture.Sha256, Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant());

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.OpenRead(stream, leaveOpen: true);
        });
    }

    /// <summary>
    /// Asserts a decoded string against the oracle's expectation, tolerating decoder-specific replacement policies
    /// for oracle values that carry U+FFFD.
    /// </summary>
    /// <param name="expected">The oracle's value.</param>
    /// <param name="actual">The reader's value.</param>
    private static void AssertOracleString(string? expected, string? actual)
    {
        if (expected is null)
        {
            Assert.IsNull(actual);
            return;
        }

        if (expected.Contains('�', StringComparison.Ordinal))
        {
            Assert.IsNotNull(actual);
            return;
        }

        Assert.AreEqual(expected, actual);
    }
}
