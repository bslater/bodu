// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies the behavior of <see cref="OlePropertySet" /> against real property-set streams and malformed input.
/// </summary>
[TestClass]
public class OlePropertySetTests
{
    /// <summary>The published format identifier of the summary-information property set.</summary>
    private static readonly Guid SummaryInformationFormatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>
    /// Verifies that reading a real summary-information stream yields a single section identified by the summary-
    /// information format identifier, with the title decodable by property identifier.
    /// </summary>
    [TestMethod]
    public void Read_WhenSummaryInformationStream_ShouldExposeFormatIdAndTitle()
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));
        using CompoundStream entry = file.RootStorage.OpenStream(SummaryInformation.StreamName);

        OlePropertySet set = OlePropertySet.Read(entry);

        Assert.AreEqual(SummaryInformationFormatId, set.FormatId);
        Assert.IsGreaterThanOrEqualTo(1, set.Sections.Count);
        Assert.AreEqual("Sample document created with MS Word", set[2]?.AsString());
    }

    /// <summary>
    /// Verifies that parsing bytes that are not a property set throws <see cref="CompoundFileFormatException" /> with
    /// the <see cref="CompoundFileError.InvalidPropertySet" /> category.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBytesAreNotPropertySet_ShouldThrowWithInvalidPropertySetCategory()
    {
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            _ = OlePropertySet.Parse(new byte[64]);
        });

        Assert.AreEqual(CompoundFileError.InvalidPropertySet, ex.Category);
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertySet.Read(Stream)" /> rejects a <see langword="null" /> stream.
    /// </summary>
    [TestMethod]
    public void Read_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => OlePropertySet.Read((Stream)null!));

        Assert.AreEqual("stream", ex.ParamName);
    }
}
