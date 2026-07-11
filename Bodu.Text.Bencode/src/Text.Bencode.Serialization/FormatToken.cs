// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Maps the format-neutral token names used by the shared serializer source (see
/// <c>Bodu.Text.Serialization/shared/</c>) to this format's <see cref="BencodeTokenType" /> members. Each Bodu
/// text-format package defines the same constant names, so shared converters compare tokens without naming the
/// per-format vocabulary.
/// </summary>
internal static class FormatToken
{
    /// <summary>The token that carries string content; in Bencode, a byte string.</summary>
    internal const BencodeTokenType String = BencodeTokenType.ByteString;

    /// <summary>The token that carries integer content.</summary>
    internal const BencodeTokenType Integer = BencodeTokenType.Integer;
}
