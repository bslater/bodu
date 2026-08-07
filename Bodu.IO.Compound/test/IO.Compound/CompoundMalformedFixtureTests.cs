// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundMalformedFixtureTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// Validates the reader against an independent corpus of deliberately malformed compound files, proving that broken
/// input is always handled safely.
/// </summary>
[TestClass]
public class CompoundMalformedFixtureTests
{
    /// <summary>
    /// Verifies that each structurally invalid fixture is rejected with a <see cref="CompoundFileFormatException" /> of
    /// the expected stable category.
    /// </summary>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.RejectedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenFixtureIsStructurallyInvalid_ShouldThrowWithExpectedCategory(CompoundMalformedKat kat)
    {
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
            using var file = CompoundFile.Open(source);
            _ = Enumerate(file.RootStorage);
        });

        Assert.AreEqual(kat.Category, ex.Category);
    }

    /// <summary>
    /// Verifies that each tolerated (recoverable) malformed fixture opens and enumerates safely without throwing.
    /// </summary>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.ToleratedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenFixtureIsRecoverable_ShouldOpenAndEnumerateSafely(CompoundMalformedKat kat)
    {
        using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
        using var file = CompoundFile.Open(source);

        int count = Enumerate(file.RootStorage);

        Assert.IsGreaterThanOrEqualTo(0, count);
    }

    /// <summary>
    /// Verifies that every fixture whose payloads are readable at the default level opens, walks, and reads through
    /// without throwing.
    /// </summary>
    /// <remarks>
    /// Enumerating metadata and reading payloads are different contracts: a defect in a sector chain is invisible
    /// until the payload it describes is materialized. Two fixtures in this corpus declare stream sizes their chains
    /// cannot satisfy and are therefore rejected only by the companion test below - they are "tolerated" only for as
    /// long as nobody reads them.
    /// </remarks>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.PayloadReadableFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadAll_WhenFixturePayloadsAreReadable_ShouldReadWithoutThrowing(CompoundMalformedKat kat)
    {
        using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
        using var file = CompoundFile.Open(source);

        long bytes = ReadAll(file.RootStorage, MaxDepth);

        Assert.IsGreaterThanOrEqualTo(0L, bytes);
    }

    /// <summary>
    /// Verifies that a fixture whose declared stream sizes outrun their sector chains is rejected with the expected
    /// category once the payloads are actually read.
    /// </summary>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.PayloadRejectedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadAll_WhenFixturePayloadIsUnreadable_ShouldThrowWithExpectedCategory(CompoundMalformedKat kat)
    {
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
            using var file = CompoundFile.Open(source);
            _ = ReadAll(file.RootStorage, MaxDepth);
        });

        Assert.AreEqual(kat.ExpectedPayloadCategory, ex.Category);
    }

    /// <summary>
    /// Verifies that the tolerant validation level opens each recoverable fixture and reads every payload without
    /// throwing.
    /// </summary>
    /// <remarks>
    /// This is the only coverage of the documented <see cref="CompoundValidationLevel.Minimal" /> contract against
    /// real-world corrupt files. It is also a termination test: recovery means the caller gets a finite, acyclic
    /// hierarchy it can walk, not merely that no exception is raised - so the walk is depth-bounded and would fail
    /// rather than hang if a pruned tree still contained a cycle.
    /// </remarks>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(60_000)]
    [DynamicData(nameof(CompoundMalformedFixtures.MinimalRecoveredFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadAll_WhenFixtureIsRecoverable_ForMinimal_ShouldReadWithoutThrowing(CompoundMalformedKat kat)
    {
        using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
        using var file = CompoundFile.Open(source, new CompoundFileOptions
        {
            ValidationLevel = CompoundValidationLevel.Minimal,
        });

        long bytes = ReadAll(file.RootStorage, MaxDepth);

        Assert.IsGreaterThanOrEqualTo(0L, bytes);
    }

    /// <summary>
    /// Verifies that the defects the tolerant level cannot recover from are still rejected with their expected
    /// category, so <see cref="CompoundValidationLevel.Minimal" /> does not degrade into accepting anything.
    /// </summary>
    /// <remarks>
    /// These are the checks that run before the validation level is consulted: a container whose signature, byte
    /// order or sector size cannot be interpreted has no recoverable reading at all, and neither does a sector
    /// identifier pointing outside the file or a root entry that is not a root storage.
    /// </remarks>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(60_000)]
    [DynamicData(nameof(CompoundMalformedFixtures.MinimalRejectedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadAll_WhenFixtureIsUnrecoverable_ForMinimal_ShouldThrowWithExpectedCategory(CompoundMalformedKat kat)
    {
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
            using var file = CompoundFile.Open(source, new CompoundFileOptions
            {
                ValidationLevel = CompoundValidationLevel.Minimal,
            });

            _ = ReadAll(file.RootStorage, MaxDepth);
        });

        Assert.AreEqual(kat.MinimalCategory, ex.Category);
    }

    /// <summary>
    /// The recursion budget for a hierarchy walk over untrusted input.
    /// </summary>
    /// <remarks>
    /// No fixture in this corpus nests anywhere near this deep, so exceeding it means the recovered hierarchy is not
    /// a tree. Bounding the walk turns that into a failed assertion instead of an uncatchable stack overflow that
    /// takes the test host with it.
    /// </remarks>
    private const int MaxDepth = 64;

    /// <summary>
    /// Recursively enumerates a storage hierarchy, returning the total entry count.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>The number of entries visited.</returns>
    private static int Enumerate(CompoundStorage storage)
    {
        int count = 0;
        foreach (CompoundEntryInfo _ in storage.EnumerateEntries())
            count++;

        foreach (CompoundStorage child in storage.EnumerateStorages())
            count += Enumerate(child);

        return count;
    }

    /// <summary>
    /// Recursively reads every stream payload in a storage hierarchy, returning the total byte count.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <param name="depth">The remaining recursion budget.</param>
    /// <returns>The number of payload bytes read.</returns>
    private static long ReadAll(CompoundStorage storage, int depth)
    {
        Assert.IsGreaterThan(0, depth, "The storage hierarchy is deeper than any fixture in this corpus; it is not a tree.");

        long bytes = 0;
        foreach (CompoundEntryInfo entry in storage.EnumerateStreams())
        {
            using CompoundStream stream = storage.OpenStream(entry.Name);
            bytes += stream.AsMemory().Length;
        }

        foreach (CompoundStorage child in storage.EnumerateStorages())
            bytes += ReadAll(child, depth - 1);

        return bytes;
    }
}
