// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Nullable{T}" /> by delegating to the underlying type's converter. YAML has a real null scalar,
/// so — unlike the shared-source twin the sibling formats compile — this converter maps a null token to
/// <see langword="null" /> before the inner converter (whose null reading would produce the underlying type's default)
/// is consulted.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class NullableConverter<T>
    : YamlConverter<T?>
    where T : struct
{
    /// <summary>The converter for the underlying type.</summary>
    private readonly YamlConverter _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableConverter{T}" /> class.
    /// </summary>
    /// <param name="inner">The converter for the underlying type.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    public NullableConverter(YamlConverter inner)
    {
        ThrowHelper.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public override T? Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? null
            : (T)_inner.ReadAsObject(ref reader, typeof(T), options)!;

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T? value, YamlSerializerOptions options) =>

        // A null value never reaches this converter — the dispatch seam writes the null scalar — so the value always
        // carries the underlying type here.
        _inner.WriteAsObject(writer, value!.Value, options);
}
