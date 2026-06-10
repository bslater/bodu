// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Nullable{T}" /> value, mapping a null token to the absent value and otherwise delegating to
/// the converter for the underlying type.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class NullableConverter<T>
    : FormatConverter<T?>
    where T : struct
{
    /// <summary>
    /// The converter for the underlying type.
    /// </summary>
    private readonly FormatConverter _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableConverter{T}" /> class.
    /// </summary>
    /// <param name="inner">The converter for the underlying type.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    public NullableConverter(FormatConverter inner)
    {
        ThrowHelper.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public override T? Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.TokenKind == SerializationValueKind.Null
            ? null
            : (T)_inner.ReadAsObject(reader, typeof(T), options) !;
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, T? value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (value is null)
            writer.WriteNull();
        else
            _inner.WriteAsObject(writer, value.Value, options);
    }
}
