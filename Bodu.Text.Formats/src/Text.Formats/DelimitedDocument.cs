// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a parsed delimited-text document, providing access to the optional header row and the ordered data rows.
/// </summary>
/// <remarks>
/// <para>
/// Obtain an instance by calling <see cref="Delimited.Parse(ReadOnlySpan{char})" /> or
/// <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" />.
/// </para>
/// <para>
/// When the source was parsed with <see cref="DelimitedParseOptions.HasHeader" /> set to <see langword="true" /> (the
/// default), the first record is treated as a header row whose fields are exposed via <see cref="Headers" />. Fields in
/// subsequent rows can then be accessed by column name using the <see cref="DelimitedRow.this[string]" /> indexer.
/// </para>
/// </remarks>
public sealed class DelimitedDocument
{

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedDocument" /> class.
    /// </summary>
    /// <param name="headers">The header field names, or an empty list when no header row was parsed.</param>
    /// <param name="rows">The ordered data rows.</param>
    /// <param name="fieldCount">The canonical field count derived from the header row or the first data row.</param>
    internal DelimitedDocument(IReadOnlyList<string> headers, IReadOnlyList<DelimitedRow> rows, int fieldCount)
    {
        Headers = headers;
        Rows = rows;
        FieldCount = fieldCount;
    }

    /// <summary>
    /// Gets the column header names from the first record, or an empty list when the document was parsed without a
    /// header row.
    /// </summary>
    /// <returns>
    /// A read-only list of header strings in source order. Empty when
    /// <see cref="DelimitedParseOptions.HasHeader" /> was <see langword="false" />.
    /// </returns>
    public IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// Gets the data rows in source order.
    /// </summary>
    /// <returns>A read-only list of <see cref="DelimitedRow" /> instances.</returns>
    public IReadOnlyList<DelimitedRow> Rows { get; }

    /// <summary>
    /// Gets the canonical field count for this document.
    /// </summary>
    /// <returns>
    /// The number of fields in the header row when <see cref="DelimitedParseOptions.HasHeader" /> was
    /// <see langword="true" />; otherwise, the field count of the first data row, or <c>0</c> when the document
    /// is empty.
    /// </returns>
    public int FieldCount { get; }
}
