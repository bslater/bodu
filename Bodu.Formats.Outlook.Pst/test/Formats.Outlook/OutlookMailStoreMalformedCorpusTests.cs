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
/// bytes are flipped or truncated, the mail reader must either decode clean or fail with the container's
/// <see cref="PstFileException" /> family or the reader's <see cref="OutlookFormatException" /> family — never
/// another exception type — at every validation level.
/// </summary>
/// <remarks>
/// This is the format-level counterpart of the container's <c>PstMalformedCorpusTests</c>: the container sweep proves
/// the node/LTP layers' exception discipline, while this sweep proves the messaging decode built on them — property
/// translation, recipient rows, attachment objects, embedded messages, bodies, and the name-to-id map — upholds the
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
    /// fails with the sanctioned exception families, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(OutlookMailStoreTests.Sample1)]
    [DataRow(OutlookMailStoreTests.Sample2Ansi)]
    public void Open_WhenBitFlipped_ShouldDecodeCleanOrThrowSanctionedFamily(string fixture)
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
                AssertDecodesCleanOrThrowsFamily(corrupted, level, $"bit {bit} flipped at offset {offset}");
        }
    }

    /// <summary>
    /// Verifies that truncation at any prefix length either decodes clean through the messaging surface or fails with
    /// the sanctioned exception families, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(OutlookMailStoreTests.Sample1)]
    [DataRow(OutlookMailStoreTests.Sample2Ansi)]
    public void Open_WhenTruncated_ShouldDecodeCleanOrThrowSanctionedFamily(string fixture)
    {
        byte[] original = PstReferenceFixtures.OpenStream(fixture).ToArray();

        var lengths = new List<int> { 0, 1, 100, 512, 563, 564, 1024 };
        for (int length = 2048; length < original.Length; length += 16007)
            lengths.Add(length);

        foreach (int length in lengths)
        {
            byte[] truncated = original.AsSpan(0, length).ToArray();

            foreach (PstValidationLevel level in s_levels)
                AssertDecodesCleanOrThrowsFamily(truncated, level, $"truncated to {length} bytes");
        }
    }

    /// <summary>
    /// Opens the supplied bytes as a mail store and decodes the complete messaging surface, failing the test if
    /// anything outside the sanctioned exception families escapes.
    /// </summary>
    /// <param name="bytes">The (possibly corrupted) file bytes.</param>
    /// <param name="level">The validation level to open with.</param>
    /// <param name="scenario">The corruption description reported on failure.</param>
    private static void AssertDecodesCleanOrThrowsFamily(byte[] bytes, PstValidationLevel level, string scenario)
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
        catch (PstFileException)
        {
            // The container's family remains sanctioned at the format level.
        }
        catch (OutlookFormatException)
        {
            // The reader's own family covers messaging-level malformation.
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
