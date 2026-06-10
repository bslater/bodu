// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelCaseNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// A naming policy that converts a member name to <c>camelCase</c> by lowercasing its first character.
/// </summary>
/// <remarks>
/// Only the first character is altered, which keeps the transformation predictable for names that contain acronyms or
/// already-lowercase leading characters.
/// </remarks>
internal sealed class CamelCaseNamingPolicy
    : BencodeNamingPolicy
{
    /// <inheritdoc />
    public override string ConvertName(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        if (name.Length == 0 || !char.IsUpper(name[0]))
            return name;

        return string.Create(name.Length, name, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
    }
}
