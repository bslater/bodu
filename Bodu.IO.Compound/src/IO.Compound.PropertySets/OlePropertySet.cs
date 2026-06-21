// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Represents a parsed OLE property set — the managed counterpart of the COM <c>IPropertyStorage</c> interface —
/// exposing its sections and the typed values they contain.
/// </summary>
/// <remarks>
/// An OLE property set is the serialized form stored in streams such as <c>\x05SummaryInformation</c>. It begins with a
/// header that declares the class identifier and one or two sections, each identified by a format identifier (FMTID)
/// and holding properties keyed by property identifier (PID).
/// </remarks>
public sealed class OlePropertySet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OlePropertySet" /> class.
    /// </summary>
    /// <param name="formatId">The format identifier of the first section.</param>
    /// <param name="classId">The class identifier declared in the property-set header.</param>
    /// <param name="codePage">The code page of the first section.</param>
    /// <param name="sections">The parsed sections, in declared order.</param>
    internal OlePropertySet(Guid formatId, Guid classId, int codePage, IReadOnlyList<OlePropertySection> sections)
    {
        FormatId = formatId;
        ClassId = classId;
        CodePage = codePage;
        Sections = sections;
    }

    /// <summary>
    /// Gets the format identifier (FMTID) of the first section.
    /// </summary>
    /// <returns>The first section's format identifier.</returns>
    public Guid FormatId { get; }

    /// <summary>
    /// Gets the class identifier declared in the property-set header.
    /// </summary>
    /// <returns>The property set's class identifier.</returns>
    public Guid ClassId { get; }

    /// <summary>
    /// Gets the code page of the first section.
    /// </summary>
    /// <returns>The first section's code page; <c>1252</c> when none is declared.</returns>
    public int CodePage { get; }

    /// <summary>
    /// Gets the sections of the property set, in declared order.
    /// </summary>
    /// <returns>A read-only list of <see cref="OlePropertySection" />.</returns>
    public IReadOnlyList<OlePropertySection> Sections { get; }

    /// <summary>
    /// Attempts to get the value of a property from the first section.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the property value; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the property exists in the first section; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetValue(int propertyId, [MaybeNullWhen(false)] out OlePropertyValue value)
    {
        if (Sections.Count > 0)
            return Sections[0].TryGetValue(propertyId, out value);

        value = null;
        return false;
    }

    /// <summary>
    /// Gets the value of a property from the first section.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <returns>The property value, or <see langword="null" /> when no such property exists.</returns>
    public OlePropertyValue? this[int propertyId] =>
        Sections.Count > 0 ? Sections[0][propertyId] : null;

    /// <summary>
    /// Parses an OLE property set from its serialized bytes.
    /// </summary>
    /// <param name="data">The raw bytes of the property-set stream.</param>
    /// <returns>The parsed <see cref="OlePropertySet" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is not a well-formed property set.
    /// </exception>
    public static OlePropertySet Parse(ReadOnlyMemory<byte> data) =>
        PropertySetReader.Read(data.Span);

    /// <summary>
    /// Reads and parses an OLE property set from a compound-file stream entry.
    /// </summary>
    /// <param name="entry">The stream entry containing the property set.</param>
    /// <returns>The parsed <see cref="OlePropertySet" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entry" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream is not a well-formed property set.
    /// </exception>
    public static OlePropertySet Read(CompoundStreamEntry entry)
    {
        ThrowHelper.ThrowIfNull(entry);

        return Parse(entry.ReadAllBytes());
    }
}
