// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstMalformedCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Pst;

/// <summary>
/// Corruption sweeps over copies of the real reference fixtures: whatever bytes are flipped or truncated, the reader
/// must either succeed or fail with the <see cref="PstFileException" /> family — never another exception type — at
/// every validation level.
/// </summary>
/// <remarks>
/// This is the "patch a copy of a real fixture" strand of the fixture strategy (exploration doc §6): the synthetic
/// <c>PstFixtureBuilder</c> cases target specific structures, while these sweeps subject the full real-file read path
/// to arbitrary damage. The walk forces every node's payload, subnodes, and — where a heap signature is present — its
/// property or table context, so corruption reaches the LTP layer, not just the header.
/// </remarks>
[TestClass]
public sealed class PstMalformedCorpusTests
{
    /// <summary>The validation levels every corruption case runs under.</summary>
    private static readonly PstValidationLevel[] s_levels =
        [PstValidationLevel.Compatible, PstValidationLevel.Strict, PstValidationLevel.Minimal];

    /// <summary>
    /// Verifies that single-bit corruption anywhere in the file either reads clean or fails with the
    /// <see cref="PstFileException" /> family, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Open_WhenBitFlipped_ShouldSucceedOrThrowPstFileException()
    {
        byte[] original = PstReferenceFixtures.OpenStream(PstFileTests.Sample1).ToArray();
        var rng = new Random(0x5EED_F1B5);

        for (int sample = 0; sample < 96; sample++)
        {
            int offset = rng.Next(original.Length);
            int bit = rng.Next(8);

            var corrupted = (byte[])original.Clone();
            corrupted[offset] ^= (byte)(1 << bit);

            foreach (PstValidationLevel level in s_levels)
                AssertReadsCleanOrThrowsFamily(corrupted, level, $"bit {bit} flipped at offset {offset}");
        }
    }

    /// <summary>
    /// Verifies that truncation at any prefix length either reads clean or fails with the
    /// <see cref="PstFileException" /> family, at every validation level.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Open_WhenTruncated_ShouldSucceedOrThrowPstFileException()
    {
        byte[] original = PstReferenceFixtures.OpenStream(PstFileTests.Sample1).ToArray();

        var lengths = new List<int> { 0, 1, 100, 512, 563, 564, 1024 };
        for (int length = 2048; length < original.Length; length += 7919)
            lengths.Add(length);

        foreach (int length in lengths)
        {
            byte[] truncated = original.AsSpan(0, length).ToArray();

            foreach (PstValidationLevel level in s_levels)
                AssertReadsCleanOrThrowsFamily(truncated, level, $"truncated to {length} bytes");
        }
    }

    /// <summary>
    /// Opens the supplied bytes and walks every node — payload, subnodes, and any property or table context — and
    /// fails the test if anything outside the <see cref="PstFileException" /> family escapes.
    /// </summary>
    /// <param name="bytes">The (possibly corrupted) file bytes.</param>
    /// <param name="level">The validation level to open with.</param>
    /// <param name="scenario">The corruption description reported on failure.</param>
    private static void AssertReadsCleanOrThrowsFamily(byte[] bytes, PstValidationLevel level, string scenario)
    {
        try
        {
            using PstFile file = PstFile.Open(
                new MemoryStream(bytes, writable: false),
                new PstFileOptions { ValidationLevel = level });

            foreach (PstNodeInfo info in file.EnumerateNodes())
            {
                PstNode node = file.GetNode(info.NodeId);
                byte[] payload = node.ReadAllBytes();
                _ = node.DataLength;

                foreach (PstNodeInfo subnode in node.EnumerateSubnodes())
                    _ = subnode.NodeId;

                if (payload.Length < 12 || payload[2] != 0xEC)
                    continue;

                if (payload[3] == 0xBC)
                {
                    PstPropertyContext context = node.ReadPropertyContext();
                    foreach (PstPropertyValue value in context)
                    {
                        _ = value.GetBytes();
                        _ = context.TryGetValueLength(value.PropertyId, out _);
                        if (context.TryOpenValueStream(value.PropertyId, out Stream? valueStream))
                        {
                            using (valueStream)
                                valueStream.CopyTo(Stream.Null);
                        }
                    }
                }
                else if (payload[3] == 0x7C)
                {
                    foreach (PstTableRow row in node.ReadTableContext().EnumerateRows())
                    {
                        foreach (PstPropertyValue cell in row.EnumerateCells())
                        {
                            _ = cell.GetBytes();
                            if (row.TryOpenCellStream(cell.PropertyId, out Stream? cellStream))
                            {
                                using (cellStream)
                                    cellStream.CopyTo(Stream.Null);
                            }
                        }
                    }
                }
            }
        }
        catch (PstFileException)
        {
            // The contract: corruption surfaces as the library's exception family.
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Reading a corrupted fixture ({scenario}, {level}) threw {ex.GetType().Name} instead of the " +
                $"PstFileException family: {ex.Message}");
        }
    }
}
