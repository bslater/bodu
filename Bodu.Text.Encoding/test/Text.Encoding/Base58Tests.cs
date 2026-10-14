// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioral tests for <see cref="Base58" />. Partial files split coverage by member or subject contract per the
/// repository test convention.
/// </summary>
[TestClass]
public sealed partial class Base58Tests
{

    /// <summary>
    /// Returns the ASCII bytes for <paramref name="value" />.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    /// <summary>
    /// Returns a run of the variant alphabet's first character, each of which decodes to one zero byte.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="count">The number of characters.</param>
    /// <returns>A string of <paramref name="count" /> leading-zero characters.</returns>
    private static string LeadingZeroCharacters(Base58Variant variant, int count) =>
        new(Base58.Encode(new byte[1], variant)[0], count);

}
