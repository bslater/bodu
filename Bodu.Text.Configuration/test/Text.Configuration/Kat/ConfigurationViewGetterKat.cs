// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewGetterKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Represents a known-answer test row for one of <see cref="ConfigurationView" />'s typed getters (<c>GetBoolean</c>,
/// <c>GetInt32</c>, <c>GetString</c>, …). A row carries the source configuration text, the key to probe, and the
/// expected strongly-typed value the view must return.
/// </summary>
/// <typeparam name="T">The strongly-typed value type returned by the getter.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Text">The configuration text supplied to the parser.</param>
/// <param name="Key">The key whose typed value is asserted.</param>
/// <param name="ExpectedValue">The expected typed value the view must return.</param>
public sealed record ConfigurationViewGetterKat<T>(
    string Name,
    string Text,
    string Key,
    T ExpectedValue) : IKat;
