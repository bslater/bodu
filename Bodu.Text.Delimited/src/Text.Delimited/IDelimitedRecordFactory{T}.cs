// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDelimitedRecordFactory{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides reflection-free conversion between a record type and its delimited field representation, consumed by the
/// <see cref="DelimitedSerializer" /> factory overloads.
/// </summary>
/// <typeparam name="TRecord">The record type.</typeparam>
/// <remarks>
/// Implementations are typically emitted at compile time by the <c>Bodu.Text.Formats.Generators</c> source generator
/// for types annotated with <see cref="DelimitedRecordAttribute" />, but the interface can equally be implemented by
/// hand when full control over the field mapping is required. Because no member of this contract inspects types at
/// runtime, the factory-based serializer paths are safe for trimming and ahead-of-time compilation.
/// </remarks>
public interface IDelimitedRecordFactory<TRecord>
{
    /// <summary>
    /// Gets the column names of the record type, in field order.
    /// </summary>
    /// <value>The resolved column names.</value>
    IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// Converts a record instance to its delimited field values, in <see cref="Headers" /> order.
    /// </summary>
    /// <param name="record">The record instance.</param>
    /// <returns>The field values.</returns>
    string[] GetFields(TRecord record);

    /// <summary>
    /// Creates a record instance from decoded field values.
    /// </summary>
    /// <param name="fields">The decoded field values of one record.</param>
    /// <param name="headers">
    /// The document's header columns, used to map fields by name; empty for a headerless document, in which case fields
    /// bind positionally in <see cref="Headers" /> order.
    /// </param>
    /// <returns>The created record.</returns>
    TRecord Create(string[] fields, IReadOnlyList<string> headers);
}
