// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySetTests.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies the two <c>WriteTo</c> sinks of <see cref="OlePropertySet" /> against
/// <see cref="OlePropertySet.ToArray" />.
/// </summary>
/// <remarks>
/// All three surfaces serialize through the same writer, so the contract worth pinning is that none of them alters,
/// truncates or re-frames the result: a caller who already owns a buffer writer or a stream must get exactly the
/// bytes <c>ToArray</c> would have returned.
/// </remarks>
public partial class OlePropertySetTests
{
    /// <summary>
    /// Builds a small authored property set with one section carrying a string and an integer.
    /// </summary>
    /// <returns>The authored set.</returns>
    private static OlePropertySet Authored()
    {
        var section = new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252);
        section.Set(2, OlePropertyValue.Create("Title value"));
        section.Set(14, OlePropertyValue.Create(42));

        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);
        set.AddSection(section);

        return set;
    }

    /// <summary>
    /// Verifies that writing to an <see cref="IBufferWriter{T}" /> produces the same bytes as
    /// <see cref="OlePropertySet.ToArray" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSinkIsBufferWriter_ShouldMatchToArray()
    {
        OlePropertySet set = Authored();
        var writer = new ArrayBufferWriter<byte>();

        set.WriteTo(writer);

        CollectionAssert.AreEqual(set.ToArray(), writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that writing to a stream appends at the current position rather than rewinding, so a caller
    /// assembling several payloads into one stream is not silently overwritten.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSinkIsStream_ShouldAppendAtCurrentPosition()
    {
        OlePropertySet set = Authored();
        byte[] expected = set.ToArray();

        using var destination = new MemoryStream();
        destination.Write(new byte[] { 0xAA, 0xBB });

        set.WriteTo(destination);

        byte[] written = destination.ToArray();
        Assert.AreEqual(expected.Length + 2, written.Length);
        CollectionAssert.AreEqual(expected, written.Skip(2).ToArray());
    }

    /// <summary>
    /// Verifies that bytes written to a stream re-parse into an equivalent property set, so the sink overloads are
    /// usable end to end rather than only byte-comparable.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSinkIsStream_ShouldRoundTripThroughParse()
    {
        using var destination = new MemoryStream();
        Authored().WriteTo(destination);

        OlePropertySet parsed = OlePropertySet.Parse(destination.ToArray());

        Assert.HasCount(1, parsed.Sections);
        Assert.AreEqual("Title value", parsed[2].AsString());
        Assert.AreEqual(42, parsed[14].AsInt32());
    }

    /// <summary>
    /// Verifies that both sinks reject a <see langword="null" /> destination before serializing.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSinkIsNull_ShouldThrowArgumentNullException()
    {
        OlePropertySet set = Authored();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.WriteTo((IBufferWriter<byte>)null!);
        });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.WriteTo((Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertySet.TryGetValue(int, out OlePropertyValue)" /> reports
    /// <see langword="false" /> when the set carries no section at all, rather than indexing an empty collection.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSetHasNoSections_ShouldReturnFalse()
    {
        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);

        Assert.IsFalse(set.TryGetValue(2, out OlePropertyValue? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that a lookup for an absent property identifier reports <see langword="false" /> even when the first
    /// section exists.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenPropertyIsAbsent_ShouldReturnFalse()
    {
        Assert.IsFalse(Authored().TryGetValue(999, out OlePropertyValue? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertySection.Remove(int)" /> drops the property and reports whether one was
    /// there, so an author can clear a property without reconstructing the section.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPropertyPresent_ShouldDropItAndReportTrue()
    {
        var section = new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252);
        section.Set(2, OlePropertyValue.Create("Title value"));

        Assert.IsTrue(section.Remove(2));
        Assert.IsFalse(section.TryGetValue(2, out _));

        Assert.IsFalse(section.Remove(2));
    }

    /// <summary>
    /// Verifies that <see cref="DocumentSummaryInformationBuilder.WriteTo(Stream)" /> writes the same bytes its
    /// property set would, and that it rejects a <see langword="null" /> stream.
    /// </summary>
    [TestMethod]
    public void DocumentSummaryBuilder_WriteTo_WhenSinkIsStream_ShouldMatchPropertySetBytes()
    {
        var builder = new DocumentSummaryInformationBuilder { Company = "Contoso" };

        using var destination = new MemoryStream();
        builder.WriteTo(destination);

        OlePropertySet parsed = OlePropertySet.Parse(destination.ToArray());
        Assert.AreEqual("Contoso", new DocumentSummaryInformation(parsed).Company);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            builder.WriteTo((Stream)null!);
        });
    }
}
