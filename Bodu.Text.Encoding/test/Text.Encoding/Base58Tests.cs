// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base58" />. Partial files split coverage by member or subject contract per the
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
}
