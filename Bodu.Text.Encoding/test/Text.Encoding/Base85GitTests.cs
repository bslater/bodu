// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the Git-style Base85 variant (<see cref="Base85Variant.GitCompact" />) and the
/// <c>EncodeGitPadded</c> / <c>DecodeGitPadded</c> helpers. Partial files split coverage by member or subject contract.
/// </summary>
[TestClass]
public sealed partial class Base85GitTests
{
    /// <summary>
    /// Returns the ASCII bytes for <paramref name="value" />.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);
}
