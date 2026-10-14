// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreMalformedCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Corruption sweeps over copies of the real reference fixture, driven through the full messaging surface: whatever
/// bytes are flipped or truncated, the mail reader must either decode clean or fail with the reader's
/// <see cref="OutlookFormatException" /> family - never another exception type, the container's
/// <see cref="PstFileException" /> family included, which arrives only as the inner exception of an
/// <see cref="OutlookPstFormatException" /> - at every validation level.
/// </summary>
/// <remarks>
/// This is the format-level counterpart of the container's <c>PstMalformedCorpusTests</c>: the container sweep proves
/// the node/LTP layers' exception discipline, while this sweep proves the messaging decode built on them - property
/// translation, recipient rows, attachment objects, embedded messages, bodies, and the name-to-id map - upholds the
/// same contract.
/// </remarks>
[TestClass]
public sealed class OutlookMailStoreMalformedCorpusTests
{
    /// <summary>The validation levels every corruption case runs under.</summary>
    private static readonly PstValidationLevel[] s_levels =
        [PstValidationLevel.Compatible, PstValidationLevel.Strict, PstValidationLevel.Minimal];

    /// <summary>
    /// Verifies that single-bit corruption anywhere in the file either decodes clean through the messaging surface or
    /// fails with the reader's <see cref="OutlookFormatException" /> family alone, at every validation level, a
    /// container fault arriving with the container's exception as its inner exception.
    /// </summary>
    /// <param name="fixture">The reference fixture to corrupt.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(OutlookMailStoreTests.Sample1)]
    [DataRow(OutlookMailStoreTests.Sample2Ansi)]
    public void Open_WhenBitFlipped_ShouldDecodeCleanOrThrowOutlookFormatException(string fixture)
    {
        byte[] original = PstReferenceFixtures.OpenStream(fixture).ToArray();
        var rng = new Random(0x0DDF_00D5);

        for (int sample = 0; sample < 64; sample++)
        {
            int offset = rng.Next(original.Length);
            int bit = rng.Next(8);

            var corrupted = (byte[])original.Clone();
            corrupted[offset] ^= (byte)(1 << bit);

            foreach (PstValidationLevel level in s_levels)
                AssertDecodesCleanOrThrowsOutlookFamily(corrupted, level, $"bit {bit} flipped at offset {offset}");
        }
    }

    /// <summary>
    /// Verifies that truncation at any prefix length either decodes clean through the messaging surface or fails with
    /// the reader's <see cref="OutlookFormatException" /> family alone, at every validation level, a container fault
    /// arriving with the container's exception as its inner exception.
    /// </summary>
    /// <param name="fixture">The reference fixture to truncate.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(OutlookMailStoreTests.Sample1)]
    [DataRow(OutlookMailStoreTests.Sample2Ansi)]
    public void Open_WhenTruncated_ShouldDecodeCleanOrThrowOutlookFormatException(string fixture)
    {
        byte[] original = PstReferenceFixtures.OpenStream(fixture).ToArray();

        var lengths = new List<int> { 0, 1, 100, 512, 563, 564, 600, 1024 };
        for (int length = 2048; length < original.Length; length += 16007)
            lengths.Add(length);

        foreach (int length in lengths)
        {
            byte[] truncated = original.AsSpan(0, length).ToArray();

            foreach (PstValidationLevel level in s_levels)
                AssertDecodesCleanOrThrowsOutlookFamily(truncated, level, $"truncated to {length} bytes");
        }
    }

    /// <summary>
    /// Opens the supplied bytes as a mail store and decodes the complete messaging surface, failing the test if an
    /// exception outside the reader's <see cref="OutlookFormatException" /> family escapes, or if an
    /// <see cref="OutlookPstFormatException" /> carries an inner exception that is not the container's.
    /// </summary>
    /// <param name="bytes">The (possibly corrupted) file bytes.</param>
    /// <param name="level">The validation level to open with.</param>
    /// <param name="scenario">The corruption description reported on failure.</param>
    private static void AssertDecodesCleanOrThrowsOutlookFamily(byte[] bytes, PstValidationLevel level, string scenario)
    {
        try
        {
            using OutlookMailStore store = OutlookMailStore.Open(
                new MemoryStream(bytes, writable: false),
                new OutlookMailStoreReaderOptions { ValidationLevel = level });

            _ = store.Properties;
            _ = store.TryGetPropertyName(new MapiPropertyTag(0x8000, MapiPropertyType.Unicode), out _);

            foreach (OutlookMailFolder folder in OutlookMailStoreTests.Walk(store.RootFolder))
            {
                _ = folder.Properties;

                foreach (OutlookMailMessage message in folder.EnumerateMessages().Concat(folder.EnumerateAssociatedMessages()))
                    DecodeMessage(message, depth: 0);
            }
        }
        catch (OutlookPstFormatException ex) when (ex.InnerException is null or PstFileException)
        {
            // The reader's own exception: a messaging-level violation, or a container fault it wraps.
        }
        catch (OutlookFormatException ex) when (ex is not OutlookPstFormatException)
        {
            // The rest of the reader's family.
        }
        catch (Exception ex)
        {
            Assert.Fail($"{scenario} at {level}: unexpected {ex.GetType().Name} (inner {ex.InnerException?.GetType().Name ?? "none"}): {ex.Message}");
        }
    }

    /// <summary>
    /// Decodes one message's complete surface, recursing into embedded messages.
    /// </summary>
    /// <param name="message">The message to decode.</param>
    /// <param name="depth">The embedded-message recursion depth, capped defensively.</param>
    private static void DecodeMessage(OutlookMailMessage message, int depth)
    {
        foreach (MapiProperty property in message.Properties)
            _ = property.Value;

        _ = message.Subject;
        _ = message.BodyText;
        _ = message.BodyHtml;
        _ = message.BodyRtf;

        foreach (OutlookRecipient recipient in message.Recipients)
        {
            foreach (MapiProperty property in recipient.Properties)
                _ = property.Value;
        }

        foreach (OutlookMailAttachment attachment in message.Attachments)
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
