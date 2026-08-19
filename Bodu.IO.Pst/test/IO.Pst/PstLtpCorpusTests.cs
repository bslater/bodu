// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstLtpCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the LTP surfaces against the reference corpus and its independently generated <c>lspst</c> oracle: folder
/// display names, message subjects and senders, contents-table row counts, and an every-node sweep proving no value
/// reference dangles. MAPI property identifiers appear here as raw literals with their MS-OXPROPS names in comments —
/// oracle knowledge in tests, kept out of the library per its layering.
/// </summary>
[TestClass]
public class PstLtpCorpusTests
{
    /// <summary>The display-name property (PidTagDisplayName, wire type 0x001F).</summary>
    private const ushort DisplayNameId = 0x3001;

    /// <summary>The subject property (PidTagSubject, wire type 0x001F).</summary>
    private const ushort SubjectId = 0x0037;

    /// <summary>The sender-name property (PidTagSenderName, wire type 0x001F).</summary>
    private const ushort SenderNameId = 0x0C1A;

    /// <summary>
    /// Strips the MS-PST subject-prefix marker: a stored subject begins with U+0001 followed by the one-character
    /// prefix-length indicator, which rendering tools such as <c>lspst</c> do not show.
    /// </summary>
    /// <param name="subject">The stored subject text.</param>
    /// <returns>The subject as an oracle listing shows it.</returns>
    private static string NormalizeSubject(string subject) =>
        subject.Length >= 2 && subject[0] == '\u0001' ? subject[2..] : subject;

    /// <summary>
    /// Gets one row per Unicode corpus fixture and validation level, since the ANSI fixtures are rejected at open.
    /// </summary>
    /// <value>The fixture/level rows.</value>
    public static IEnumerable<object[]> UnicodeFixtureLevelRows =>
        from fixture in PstReferenceFixtures.Manifest.Fixtures
        where fixture.Format == "unicode"
        from level in new[] { PstValidationLevel.Compatible, PstValidationLevel.Strict }
        select new object[] { fixture, level };

    /// <summary>
    /// Verifies that every user folder the <c>lspst</c> oracle lists is present as a folder node whose property
    /// context carries that display name.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    /// <param name="level">The validation level to open under.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(UnicodeFixtureLevelRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadPropertyContext_WhenCorpusFolders_ShouldCarryOracleDisplayNames(PstReferenceFixture fixture, PstValidationLevel level)
    {
        var expectedFolders = fixture.LspstListing
            .Where(static line => line.StartsWith("Folder \"", StringComparison.Ordinal))
            .Select(static line => line["Folder \"".Length..^1])
            .ToList();

        using PstFile file = PstReferenceFixtures.OpenFile(fixture.File, level);
        var folderNames = new List<string>();
        foreach (PstNodeInfo info in file.EnumerateNodes().Where(static n => n.NodeId.Type == PstNodeType.NormalFolder))
        {
            PstPropertyContext context = file.GetNode(info.NodeId).ReadPropertyContext();
            if (context.TryGetValue(DisplayNameId, out PstPropertyValue name))
                folderNames.Add(name.GetString());
        }

        foreach (string expected in expectedFolders)
            Assert.IsTrue(folderNames.Contains(expected), $"Folder '{expected}' not found among [{string.Join(", ", folderNames)}].");
    }

    /// <summary>
    /// Verifies that every message the <c>lspst</c> oracle lists is present as a message node whose property context
    /// carries the oracle's subject and sender name, and that the message-node census is at least the oracle's count.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    /// <param name="level">The validation level to open under.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(UnicodeFixtureLevelRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadPropertyContext_WhenCorpusMessages_ShouldCarryOracleSubjectsAndSenders(PstReferenceFixture fixture, PstValidationLevel level)
    {
        var expectedMessages = fixture.LspstListing
            .Where(static line => line.StartsWith("Email\t", StringComparison.Ordinal))
            .Select(static line =>
            {
                string[] parts = line.Split('\t');
                return (From: parts[1]["From: ".Length..], Subject: parts[2]["Subject: ".Length..]);
            })
            .ToList();

        using PstFile file = PstReferenceFixtures.OpenFile(fixture.File, level);
        var messages = new List<(string From, string Subject)>();
        foreach (PstNodeInfo info in file.EnumerateNodes().Where(static n => n.NodeId.Type == PstNodeType.NormalMessage))
        {
            PstPropertyContext context = file.GetNode(info.NodeId).ReadPropertyContext();
            messages.Add((
                context.TryGetValue(SenderNameId, out PstPropertyValue sender) ? sender.GetString() : string.Empty,
                context.TryGetValue(SubjectId, out PstPropertyValue subject) ? NormalizeSubject(subject.GetString()) : string.Empty));
        }

        // The oracle count is a floor: lspst counts only the items it classifies as Email, and one fixture carries
        // message nodes of other classes it reported as zero.
        Assert.IsTrue(messages.Count >= fixture.MessageCount);
        foreach ((string from, string subject) in expectedMessages)
        {
            Assert.IsTrue(
                messages.Any(m => m.Subject == subject && m.From == from),
                $"Message '{subject}' from '{from}' not found among [{string.Join("; ", messages)}].");
        }
    }

    /// <summary>
    /// Verifies that the contents tables hold at least the oracle's message count, that every oracle-listed subject
    /// appears among the rows, and that each row's identifier resolves to a message node carrying a subject.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    /// <param name="level">The validation level to open under.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(UnicodeFixtureLevelRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadTableContext_WhenCorpusContentsTables_ShouldMatchOracleMessageCounts(PstReferenceFixture fixture, PstValidationLevel level)
    {
        var expectedSubjects = fixture.LspstListing
            .Where(static line => line.StartsWith("Email\t", StringComparison.Ordinal))
            .Select(static line => line.Split('\t')[2]["Subject: ".Length..])
            .ToList();

        using PstFile file = PstReferenceFixtures.OpenFile(fixture.File, level);
        int totalRows = 0;
        var rowSubjects = new List<string>();
        foreach (PstNodeInfo info in file.EnumerateNodes().Where(static n => n.NodeId.Type == PstNodeType.ContentsTable))
        {
            PstTableContext table = file.GetNode(info.NodeId).ReadTableContext();
            foreach (PstTableRow row in table.EnumerateRows())
            {
                totalRows++;

                // A contents-table row identifier is the message's node identifier.
                Assert.IsTrue(file.TryGetNode(new PstNodeId(row.RowId), out PstNode? message));
                PstPropertyContext context = message.ReadPropertyContext();
                Assert.IsTrue(context.TryGetValue(SubjectId, out PstPropertyValue subject));
                rowSubjects.Add(NormalizeSubject(subject.GetString()));
            }
        }

        // Every oracle-listed message must appear in a contents table; the tables may also hold message classes the
        // oracle did not count, so the row total is a floor rather than an exact match.
        Assert.IsTrue(totalRows >= fixture.MessageCount);
        foreach (string expected in expectedSubjects)
            Assert.IsTrue(rowSubjects.Contains(expected), $"Subject '{expected}' not found among [{string.Join("; ", rowSubjects)}].");
    }

    /// <summary>
    /// Verifies the no-dangling-reference invariant: every node whose payload declares the heap signature opens as
    /// the structure its client signature names, and every property value and table cell resolves without throwing.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    /// <param name="level">The validation level to open under.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(UnicodeFixtureLevelRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EnumerateNodes_WhenEveryHeapNodeIsRead_ShouldResolveEveryValueReference(PstReferenceFixture fixture, PstValidationLevel level)
    {
        using PstFile file = PstReferenceFixtures.OpenFile(fixture.File, level);
        int propertyContexts = 0;
        int tableContexts = 0;

        foreach (PstNodeInfo info in file.EnumerateNodes())
        {
            PstNode node = file.GetNode(info.NodeId);
            byte[] payload = node.ReadAllBytes();
            if (payload.Length < 12 || payload[2] != 0xEC)
                continue;

            switch (payload[3])
            {
                case 0xBC:
                    foreach (PstPropertyValue value in node.ReadPropertyContext())
                        _ = value.GetBytes();

                    propertyContexts++;
                    break;

                case 0x7C:
                    foreach (PstTableRow row in node.ReadTableContext().EnumerateRows())
                    {
                        foreach (PstPropertyValue cell in row.EnumerateCells())
                            _ = cell.GetBytes();
                    }

                    tableContexts++;
                    break;
            }
        }

        Assert.IsTrue(propertyContexts > 0, "The fixture carries no property contexts.");
        Assert.IsTrue(tableContexts > 0, "The fixture carries no table contexts.");
    }
}
