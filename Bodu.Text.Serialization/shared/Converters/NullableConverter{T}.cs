// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Converts a <see cref="Nullable{T}" /> value by delegating to the converter for the underlying type.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
/// <remarks>
/// The format has no null token. A member whose value is <see langword="null" /> is omitted from the output by the
/// enclosing dictionary converter before the value converter is consulted, so this converter only ever reads or writes
/// a present value and forwards it to the underlying converter.
/// </remarks>
internal sealed class NullableConverter<T>
    : SharedConverter<T?>
    where T : struct
{
    /// <summary>The converter for the underlying type.</summary>
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
    public override T? Read(ref FormatReader reader, Type typeToConvert, FormatOptions options) =>
        (T)_inner.ReadAsObject(ref reader, typeof(T), options)!;

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T? value, FormatOptions options) =>
        _inner.WriteAsObject(writer, value!.Value, options);
}
