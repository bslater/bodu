// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChildBinder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Reads a child value on behalf of a container converter, attaching the child's path segment to any binding failure
/// so the reported <see cref="YamlSerializationException.Path" /> identifies the offending member, index, or key.
/// </summary>
internal static class ChildBinder
{
    /// <summary>
    /// Reads a child value keyed by a member or mapping-key segment.
    /// </summary>
    /// <param name="reader">The reader positioned on the child value's first token.</param>
    /// <param name="converter">The converter for the child's declared type.</param>
    /// <param name="type">The child's declared type.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="segment">The path segment for this child (a member or key name).</param>
    /// <returns>The bound value.</returns>
    /// <exception cref="YamlSerializationException">The child cannot be bound to the target type.</exception>
    internal static object? Read(ref Utf8YamlReader reader, YamlConverter converter, Type type, YamlSerializerOptions options, string segment)
    {
        try
        {
            return converter.ReadAsObject(ref reader, type, options);
        }
        catch (YamlSerializationException ex)
        {
            ex.Path = YamlSerializationException.CombinePath(segment, ex.Path);
            throw;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException or ArgumentException)
        {
            throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, type), ex)
            {
                Path = segment,
            };
        }
    }

    /// <summary>
    /// Reads a child value keyed by a sequence index. The <c>[i]</c> segment text is produced only when a failure
    /// occurs, so the success path allocates nothing for the segment.
    /// </summary>
    /// <param name="reader">The reader positioned on the child value's first token.</param>
    /// <param name="converter">The converter for the element's declared type.</param>
    /// <param name="type">The element's declared type.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="index">The zero-based sequence index of this child.</param>
    /// <returns>The bound value.</returns>
    /// <exception cref="YamlSerializationException">The child cannot be bound to the target type.</exception>
    internal static object? Read(ref Utf8YamlReader reader, YamlConverter converter, Type type, YamlSerializerOptions options, int index)
    {
        try
        {
            return converter.ReadAsObject(ref reader, type, options);
        }
        catch (YamlSerializationException ex)
        {
            ex.Path = YamlSerializationException.CombinePath(IndexSegment(index), ex.Path);
            throw;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException or ArgumentException)
        {
            throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, type), ex)
            {
                Path = IndexSegment(index),
            };
        }
    }

    /// <summary>
    /// Produces the <c>[i]</c> path segment for a sequence index.
    /// </summary>
    /// <param name="index">The zero-based sequence index.</param>
    /// <returns>The segment text.</returns>
    private static string IndexSegment(int index) =>
        string.Create(CultureInfo.InvariantCulture, $"[{index}]");
}
