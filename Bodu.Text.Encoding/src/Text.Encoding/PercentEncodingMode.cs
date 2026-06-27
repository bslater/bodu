// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the set of characters <see cref="PercentEncoding" /> leaves unescaped, selecting the URI component or
/// form-encoding rules to apply.
/// </summary>
public enum PercentEncodingMode : byte
{
    /// <summary>
    /// Encodes every byte except the RFC 3986 unreserved set (<c>ALPHA</c> / <c>DIGIT</c> / <c>-</c> / <c>.</c> /
    /// <c>_</c> / <c>~</c>). The safest default for an independent value embedded into any URI component.
    /// </summary>
    UriComponent = 0,

    /// <summary>
    /// Encodes for a single RFC 3986 path segment: passes through unreserved characters, sub-delimiters, <c>:</c>, and
    /// <c>@</c>, while encoding <c>/</c>, <c>?</c>, and <c>#</c>. Encode each segment separately and join with <c>/</c>.
    /// </summary>
    PathSegment = 1,

    /// <summary>
    /// Encodes for a whole RFC 3986 query component: passes through unreserved characters, sub-delimiters, <c>:</c>,
    /// <c>@</c>, <c>/</c>, and <c>?</c>, while encoding <c>#</c>. For individual parameter names and values prefer
    /// <see cref="UriComponent" /> or <see cref="FormUrlEncoded" />.
    /// </summary>
    Query = 2,

    /// <summary>
    /// Encodes per the WHATWG <c>application/x-www-form-urlencoded</c> percent-encode set: passes through only ASCII
    /// alphanumerics and <c>*</c>, <c>-</c>, <c>.</c>, <c>_</c>; encodes space as <c>+</c>, a literal plus as
    /// <c>%2B</c>, and a literal percent as <c>%25</c>.
    /// </summary>
    FormUrlEncoded = 3,
}
