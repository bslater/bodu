// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.WritePropertySet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>The format identifier used for the write-back test sections.</summary>
    private static readonly Guid WriteBackFormatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>
    /// Builds a single-section property set carrying one string property at identifier 2.
    /// </summary>
    /// <param name="text">The string value to store.</param>
    /// <returns>The property set.</returns>
    private static OlePropertySet CreatePropertySet(string text)
    {
        var section = new OlePropertySection(WriteBackFormatId, 1252);
        section.Set(2, OlePropertyValue.Create(text));
        var set = new OlePropertySet(WriteBackFormatId, Guid.Empty, 1252);
        set.AddSection(section);
        return set;
    }

    /// <summary>
    /// Verifies that a property set written to a new stream reads back through
    /// <see cref="CompoundStorage.TryOpenPropertySet(string, out OlePropertySet)" /> after commit and reopen.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenNew_ShouldRoundTripAfterCommit()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.WritePropertySet("Custom", CreatePropertySet("payload"));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.RootStorage.TryOpenPropertySet("Custom", out OlePropertySet? parsed));
        Assert.AreEqual("payload", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that writing a property set over an existing stream replaces the previous payload.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenStreamExists_ShouldReplaceContent()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.WritePropertySet("Custom", CreatePropertySet("first"));
            file.RootStorage.WritePropertySet("Custom", CreatePropertySet("second"));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.RootStorage.TryOpenPropertySet("Custom", out OlePropertySet? parsed));
        Assert.AreEqual("second", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that a property set written to a nested storage lands in that storage rather than the root.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenNestedStorage_ShouldCreateStreamInThatStorage()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.WritePropertySet("Custom", CreatePropertySet("nested"));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsFalse(reopened.RootStorage.TryOpenPropertySet("Custom", out _));
        CompoundStorage storage = reopened.RootStorage.OpenStorage("Embedded");
        Assert.IsTrue(storage.TryOpenPropertySet("Custom", out OlePropertySet? parsed));
        Assert.AreEqual("nested", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that writing a property set on a read-only compound file throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenReadOnlyFile_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.RootStorage.WritePropertySet("Custom", CreatePropertySet("x"));
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> stream name throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenNameNull_ShouldThrowArgumentNullException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            file.RootStorage.WritePropertySet(null!, CreatePropertySet("x"));
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> property set throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertySet_WhenPropertySetNull_ShouldThrowArgumentNullException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            file.RootStorage.WritePropertySet("Custom", null!);
        });

        Assert.AreEqual("propertySet", ex.ParamName);
    }
}
