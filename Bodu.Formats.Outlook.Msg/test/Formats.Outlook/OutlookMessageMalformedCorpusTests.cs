// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageMalformedCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Corruption sweeps over copies of the real reference corpus, driven through the full message surface: whatever
/// bytes are flipped or truncated, the reader must either decode clean or fail with the
/// <see cref="OutlookFormatException" /> family — never another exception type — at every validation level.
/// </summary>
/// <remarks>
/// This is the <c>.msg</c> counterpart of the PST reader's <c>OutlookMailStoreMalformedCorpusTests</c>. The
/// container beneath the reader has its own hardening suite; this sweep proves that container failures surfacing
/// after <see cref="OutlookMessage.Open(Stream, OutlookMessageReaderOptions, bool)" /> returns — a broken sector
/// chain met while reading a recipient, an attachment payload, or a named-property stream — are translated into the
/// reader's documented exception contract rather than escaping as the container's own types.
/// </remarks>
[TestClass]
public sealed class OutlookMessageMalformedCorpusTests
{
    /// <summary>The validation levels every corruption case runs under.</summary>
    private static readonly CompoundValidationLevel[] s_levels =
        [CompoundValidationLevel.Compatible, CompoundValidationLevel.Strict];

    /// <summary>
    /// Verifies that single-bit corruption anywhere in a valid corpus file either decodes clean through the message
    /// surface or fails with the reader's exception family, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Open_WhenBitFlipped_ShouldDecodeCleanOrThrowSanctionedFamily()
    {
        var rng = new Random(0x0BAD_F00D);

        foreach (MsgReferenceFixture fixture in ValidFixtures())
        {
            byte[] original = MsgReferenceFixtures.OpenStream("valid", fixture.File).ToArray();

            for (int sample = 0; sample < 12; sample++)
            {
                int offset = rng.Next(original.Length);
                int bit = rng.Next(8);

                var corrupted = (byte[])original.Clone();
                corrupted[offset] ^= (byte)(1 << bit);

                foreach (CompoundValidationLevel level in s_levels)
                    AssertDecodesCleanOrThrowsFamily(corrupted, level, $"{fixture.File}: bit {bit} flipped at offset {offset}");
            }
        }
    }

    /// <summary>
    /// Verifies that truncation at any prefix length either decodes clean through the message surface or fails with
    /// the reader's exception family, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Open_WhenTruncated_ShouldDecodeCleanOrThrowSanctionedFamily()
    {
        foreach (MsgReferenceFixture fixture in ValidFixtures())
        {
            byte[] original = MsgReferenceFixtures.OpenStream("valid", fixture.File).ToArray();

            var lengths = new List<int> { 0, 1, 512, 1024 };
            for (int length = 1536; length < original.Length; length += 4099)
                lengths.Add(length);

            foreach (int length in lengths)
            {
                byte[] truncated = original.AsSpan(0, length).ToArray();

                foreach (CompoundValidationLevel level in s_levels)
                    AssertDecodesCleanOrThrowsFamily(truncated, level, $"{fixture.File}: truncated to {length} bytes");
            }
        }
    }

    /// <summary>
    /// Enumerates the corpus rows classified as valid messages.
    /// </summary>
    /// <returns>The valid fixture rows.</returns>
    private static IEnumerable<MsgReferenceFixture> ValidFixtures() =>
        MsgReferenceFixtures.Manifest.Fixtures.Where(static f => f.Expected == "valid");

    /// <summary>
    /// Opens the supplied bytes as a message and decodes the complete surface, failing the test if anything outside
    /// the sanctioned exception family escapes.
    /// </summary>
    /// <param name="bytes">The (possibly corrupted) file bytes.</param>
    /// <param name="level">The validation level to open with.</param>
    /// <param name="scenario">The corruption description reported on failure.</param>
    private static void AssertDecodesCleanOrThrowsFamily(byte[] bytes, CompoundValidationLevel level, string scenario)
    {
        try
        {
            using var message = OutlookMessage.Open(
                new MemoryStream(bytes, writable: false),
                new OutlookMessageReaderOptions { ValidationLevel = level });

            DecodeMessage(message, depth: 0);
        }
        catch (OutlookFormatException)
        {
            // The reader's own family is the documented contract.
        }
        catch (Exception ex)
        {
            Assert.Fail($"{scenario} at {level}: unexpected {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Decodes one message's complete surface, recursing into embedded messages.
    /// </summary>
    /// <param name="message">The message to decode.</param>
    /// <param name="depth">The embedded-message recursion depth, capped defensively.</param>
    private static void DecodeMessage(OutlookMessage message, int depth)
    {
        foreach (MapiProperty property in message.Properties)
            _ = property.Value;

        _ = message.Subject;
        _ = message.BodyText;
        _ = message.BodyHtml;
        _ = message.BodyRtf;
        _ = message.TryGetPropertyName(new MapiPropertyTag(0x8000, MapiPropertyType.Unicode), out _);

        foreach (OutlookRecipient recipient in message.Recipients)
        {
            foreach (MapiProperty property in recipient.Properties)
                _ = property.Value;
        }

        foreach (OutlookAttachment attachment in message.Attachments)
        {
            foreach (MapiProperty property in attachment.Properties)
                _ = property.Value;

            if (attachment.Method == OutlookAttachmentMethod.ByValue)
            {
                using Stream content = attachment.OpenContentStream();
                content.CopyTo(Stream.Null);
            }
            else if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage && depth < 8)
            {
                DecodeMessage(attachment.OpenMessage(), depth + 1);
            }
        }
    }
}
