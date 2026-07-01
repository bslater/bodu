// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies hierarchy navigation through <see cref="CompoundStorage" />, including scoped child lookups.
/// </summary>
[TestClass]
public class CompoundStorageTests
{
    /// <summary>
    /// Opens the reference fixture whose root contains a single nested storage <c>Storage 1</c> with one stream.
    /// </summary>
    /// <returns>An open compound file over <c>valid/clean.dat</c>.</returns>
    private static CompoundFile OpenNested() =>
        CompoundFile.Open(CompoundFixtures.OpenReference("valid/clean.dat"));

    /// <summary>
    /// Verifies that the root storage enumerates its direct child storage but not the grandchild stream.
    /// </summary>
    [TestMethod]
    public void EnumerateStorages_WhenNestedFixture_ShouldReturnOnlyDirectChildStorage()
    {
        using CompoundFile file = OpenNested();

        var storages = file.RootStorage.EnumerateStorages().ToList();

        Assert.HasCount(1, storages);
        Assert.AreEqual("Storage 1", storages[0].Name);
    }

    /// <summary>
    /// Verifies that a nested storage enumerates its own child streams.
    /// </summary>
    [TestMethod]
    public void EnumerateStreams_WhenNestedStorage_ShouldReturnChildStreams()
    {
        using CompoundFile file = OpenNested();

        CompoundStorage storage1 = file.RootStorage.OpenStorage("Storage 1");
        var streams = storage1.EnumerateStreams().ToList();

        Assert.HasCount(1, streams);
        Assert.AreEqual("Stream 1", streams[0].Name);
    }

    /// <summary>
    /// Verifies that child lookups are scoped to a storage: a stream nested under a child storage is not visible at the
    /// root, but is reachable through its parent storage.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenStreamNestedUnderChildStorage_ShouldBeScopedToParent()
    {
        using CompoundFile file = OpenNested();

        Assert.IsFalse(file.RootStorage.TryOpenStream("Stream 1", out _));
        Assert.IsTrue(file.RootStorage.OpenStorage("Storage 1").TryOpenStream("Stream 1", out CompoundStream? nested));
        Assert.IsNotNull(nested);
        Assert.AreEqual("Stream 1", nested.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStorage(string)" /> throws
    /// <see cref="CompoundStreamNotFoundException" /> for a missing storage.
    /// </summary>
    [TestMethod]
    public void OpenStorage_WhenStorageMissing_ShouldThrowCompoundStreamNotFoundException()
    {
        using CompoundFile file = OpenNested();

        _ = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() => file.RootStorage.OpenStorage("Nope"));
    }

    /// <summary>
    /// Verifies that child storage lookups match names case-insensitively, as the compound-file format defines.
    /// </summary>
    [TestMethod]
    public void TryOpenStorage_WhenNameDiffersOnlyByCase_ShouldResolve()
    {
        using CompoundFile file = OpenNested();

        Assert.IsTrue(file.RootStorage.TryOpenStorage("STORAGE 1", out CompoundStorage? storage));
        Assert.IsNotNull(storage);
        Assert.AreEqual("Storage 1", storage.Name);
    }

    /// <summary>
    /// Verifies that child stream lookups match names case-insensitively, so a case-mismatched name still resolves.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenNameDiffersOnlyByCase_ShouldResolve()
    {
        using CompoundFile file = OpenNested();

        CompoundStorage storage1 = file.RootStorage.OpenStorage("Storage 1");

        Assert.IsTrue(storage1.TryOpenStream("stream 1", out CompoundStream? stream));
        Assert.IsNotNull(stream);
        Assert.AreEqual("Stream 1", stream.Name);
    }

    /// <summary>
    /// Verifies that the root storage enumerates the expected top-level streams of an Excel workbook fixture.
    /// </summary>
    [TestMethod]
    public void EnumerateStreams_WhenWorkbookFixture_ShouldExposeWorkbookStream()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.xls"));

        Assert.IsTrue(file.RootStorage.TryOpenStream("Workbook", out CompoundStream? workbook));
        Assert.IsNotNull(workbook);
        Assert.IsGreaterThan(0, workbook.Length);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.EnumerateEntries" /> returns both the child storage and stream of a
    /// fixture with two nested streams.
    /// </summary>
    [TestMethod]
    public void EnumerateStreams_WhenStorageHasTwoStreams_ShouldReturnBoth()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/example2.dat"));

        CompoundStorage storage1 = file.RootStorage.OpenStorage("Storage 1");
        var names = storage1.EnumerateStreams().Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

        CollectionAssert.AreEqual(new[] { "Stream 1", "Stream 2" }, names);
    }
}
