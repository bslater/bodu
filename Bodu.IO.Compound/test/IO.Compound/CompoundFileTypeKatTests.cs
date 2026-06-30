// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTypeKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// A known-answer row pairing a well-known COM/OLE application file type with its published root-storage class identifier
/// and a representative reference fixture.
/// </summary>
/// <param name="Description">The human-readable file-type description, for example <c>Microsoft Word 97-2003</c>.</param>
/// <param name="RelativePath">The corpus-relative fixture path.</param>
/// <param name="RootClsid">The published root-storage class identifier for the file type.</param>
public sealed record CompoundFileTypeKat(string Description, string RelativePath, Guid RootClsid)
    : IKat
{
    /// <inheritdoc />
    public string Name => $"{Description} ({RelativePath["valid/".Length..]})";
}

/// <summary>
/// Validates that the reader surfaces the published root-storage class identifier (CLSID) of well-known COM/OLE document
/// types, using known answers drawn from the Microsoft OLE class registrations.
/// </summary>
[TestClass]
public class CompoundFileTypeKatTests
{
    /// <summary>The well-known file-type rows.</summary>
    private static readonly CompoundFileTypeKat[] s_types =
    [
        new("Microsoft Word 97-2003 Document", "valid/sample1.doc", new Guid("00020906-0000-0000-C000-000000000046")),
        new("Microsoft Word 97-2003 Document", "valid/sample2.doc", new Guid("00020906-0000-0000-C000-000000000046")),
        new("Microsoft Word 97-2003 Document", "valid/test-ole-file.doc", new Guid("00020906-0000-0000-C000-000000000046")),
        new("Microsoft Excel 97-2003 Worksheet", "valid/sample1.xls", new Guid("00020820-0000-0000-C000-000000000046")),
    ];

    /// <summary>
    /// Gets the well-known file-type rows as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping file-type rows.</returns>
    public static IEnumerable<object[]> FileTypes()
    {
        foreach (CompoundFileTypeKat kat in s_types)
            yield return [kat];
    }

    /// <summary>
    /// Verifies that the root storage of each well-known document type exposes the type's published CLSID.
    /// </summary>
    /// <param name="kat">The file-type row.</param>
    [TestMethod]
    [DynamicData(nameof(FileTypes), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RootStorage_WhenWellKnownFileType_ShouldExposePublishedClassId(CompoundFileTypeKat kat)
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference(kat.RelativePath));

        Assert.AreEqual(kat.RootClsid, file.RootStorage.Stat.ClassId);
    }
}
