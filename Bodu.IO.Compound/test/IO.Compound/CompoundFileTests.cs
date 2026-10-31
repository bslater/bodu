// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the behavior of <see cref="CompoundFile" /> against built and reference compound files.
/// </summary>
[TestClass]
public partial class CompoundFileTests
{
    /// <summary>
    /// The write limit the fault-injected destinations carry. A version-3 container is written one 512-byte sector at
    /// a time, so the third sector crosses it: the destination keeps the header and the first directory sector, 1,024
    /// bytes, when the commit faults, and 1,536 bytes when the commit is canceled after the sector that reaches it.
    /// </summary>
    private const long MidWriteFaultBytes = 1500;

    /// <summary>
    /// Opens the sample compound file the tests build.
    /// </summary>
    /// <returns>An open <see cref="CompoundFile" /> over the sample.</returns>
    private static CompoundFile OpenSample() =>
        CompoundFile.Open(CompoundFixtures.OpenSample());

    /// <summary>
    /// Builds the committed container the fault-injection tests open for update: a mini stream and a regular-sector
    /// stream at the root.
    /// </summary>
    /// <returns>The committed container.</returns>
    private static byte[] CreateSeedContainer() =>
        CreateContainer(("A", CompoundContainerBytes.Pattern(100)), ("B", CompoundContainerBytes.Pattern(3000))).ToArray();

    /// <summary>
    /// Stages the edits the fault-injection tests apply to a container opened for update: the seed's mini stream is
    /// removed, a root stream is added, and a storage gains a regular-sector stream of 6,000 bytes and a mini stream,
    /// so the committed container runs to several sectors.
    /// </summary>
    /// <param name="file">The file opened for update over the seed container.</param>
    private static void StageSampleEdits(CompoundFile file)
    {
        Assert.IsTrue(file.RootStorage.Delete("A"));
        file.RootStorage.CreateStream("Meta", new byte[] { 1, 2, 3, 4 });
        CompoundStorage data = file.RootStorage.CreateStorage("Data");
        data.CreateStream("Large", CreatePayload(6000));
        data.CreateStream("Small", CreatePayload(64));
    }

    /// <summary>
    /// Commits the sample edits over a copy of a container through a plain <see cref="MemoryStream" />, giving the
    /// bytes a fault-free commit of the same tree produces.
    /// </summary>
    /// <param name="seed">The committed container the copy is opened for update over.</param>
    /// <returns>The committed container.</returns>
    private static byte[] CommitSampleEditsToMemory(byte[] seed)
    {
        using var destination = new MemoryStream();
        destination.Write(seed);
        destination.Position = 0;
        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            StageSampleEdits(file);
            file.Commit();
        }

        return destination.ToArray();
    }

    /// <summary>
    /// Asserts that a destination holds a partially written container: more than nothing, less than the whole, and
    /// bytes the reader rejects with <see cref="CompoundFileFormatException" /> rather than opening.
    /// </summary>
    /// <param name="partial">The bytes the destination holds.</param>
    /// <param name="full">The bytes a fault-free commit produces.</param>
    private static void AssertPartiallyWritten(byte[] partial, byte[] full)
    {
        Assert.IsGreaterThan(0, partial.Length);
        Assert.IsLessThan(full.Length, partial.Length);
        Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using CompoundFile opened = CompoundFile.Open(new MemoryStream(partial));
        });
    }

    /// <summary>
    /// Asserts that a committed container reopens with the sample edits: the seed's mini stream gone, its
    /// regular-sector stream kept, and the added root stream, storage and storage streams holding their bytes.
    /// </summary>
    /// <param name="container">The committed container.</param>
    private static void AssertHoldsSampleEdits(byte[] container)
    {
        using var reopened = CompoundFile.Open(new MemoryStream(container));

        Assert.IsFalse(reopened.RootStorage.TryOpenStream("A", out _));
        using CompoundStream kept = reopened.RootStorage.OpenStream("B");
        CollectionAssert.AreEqual(CompoundContainerBytes.Pattern(3000), kept.ReadAllBytes());
        using CompoundStream meta = reopened.RootStorage.OpenStream("Meta");
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, meta.ReadAllBytes());
        CompoundStorage data = reopened.RootStorage.OpenStorage("Data");
        using CompoundStream large = data.OpenStream("Large");
        CollectionAssert.AreEqual(CreatePayload(6000), large.ReadAllBytes());
        using CompoundStream small = data.OpenStream("Small");
        CollectionAssert.AreEqual(CreatePayload(64), small.ReadAllBytes());
    }

    /// <summary>
    /// Asserts that a committed container reopens with exactly the entries of the seed container, every stream holding
    /// the bytes the seed's stream of that name holds.
    /// </summary>
    /// <param name="container">The committed container.</param>
    private static void AssertHoldsSeedContainer(byte[] container)
    {
        using var reopened = CompoundFile.Open(new MemoryStream(container));
        using var original = CompoundFile.Open(new MemoryStream(CreateSeedContainer()));

        string[] expectedNames = original.RootStorage.EnumerateEntries().Select(static entry => entry.Name).Order().ToArray();
        string[] actualNames = reopened.RootStorage.EnumerateEntries().Select(static entry => entry.Name).Order().ToArray();
        CollectionAssert.AreEqual(expectedNames, actualNames);
        Assert.IsEmpty(reopened.RootStorage.EnumerateStorages());

        foreach (CompoundEntryInfo entry in original.RootStorage.EnumerateStreams())
        {
            using CompoundStream expected = original.RootStorage.OpenStream(entry.Name);
            using CompoundStream actual = reopened.RootStorage.OpenStream(entry.Name);
            CollectionAssert.AreEqual(expected.ReadAllBytes(), actual.ReadAllBytes(), entry.Name);
        }
    }

    /// <summary>
    /// Verifies that opening a valid compound file yields a readable instance whose root storage exposes entries.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Open_WhenStreamIsValidCompoundFile_ShouldReturnReadableInstance()
    {
        using CompoundFile file = OpenSample();

        Assert.IsNotEmpty(file.RootStorage.EnumerateEntries());
    }
}
