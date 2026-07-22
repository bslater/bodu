// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Enumerates the token kinds a <see cref="Bodu.Text.DotEnv.Reader.Utf8DotEnvReader" /> reports as it advances through
/// a DotEnv document.
/// </summary>
/// <remarks>
/// DotEnv has a single flat structure — an ordered object of string-valued keys — so the token vocabulary is
/// deliberately small: a synthetic document object frames the key/value pairs, and every value is a
/// <see cref="String" /> scalar. There is no array form.
/// </remarks>
public enum DotEnvTokenType
{
    /// <summary>
    /// No token has been read, or the reader has reached the end of the document.
    /// </summary>
    None = 0,

    /// <summary>
    /// The start of the synthetic document object, emitted before the first key.
    /// </summary>
    StartObject,

    /// <summary>
    /// The end of the synthetic document object, emitted after the last key.
    /// </summary>
    EndObject,

    /// <summary>
    /// A key name, emitted before its value.
    /// </summary>
    PropertyName,

    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// A comment line, surfaced for tooling and the mutable document object; skipped by the binder.
    /// </summary>
    Comment,
}
