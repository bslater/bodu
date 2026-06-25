// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Nullable{T}" /> value by delegating to the converter for the underlying type.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
/// <remarks>
/// Bencode has no null token. A member whose value is <see langword="null" /> is omitted from the output by the
/// enclosing dictionary converter before the value converter is consulted, so this converter only ever reads or writes
/// a present value and forwards it to the underlying converter.
/// </remarks>
internal sealed class NullableConverter<T>
    : BencodeConverter<T?>
    where T : struct
{
    /// <summary>The converter for the underlying type.</summary>
    private readonly BencodeConverter _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableConverter{T}" /> class.
    /// </summary>
    /// <param name="inner">The converter for the underlying type.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    public NullableConverter(BencodeConverter inner)
    {
        ThrowHelper.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public override T? Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
        (T)_inner.ReadAsObject(ref reader, typeof(T), options) !;

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T? value, BencodeSerializerOptions options) =>
        _inner.WriteAsObject(writer, value!.Value, options);
}
