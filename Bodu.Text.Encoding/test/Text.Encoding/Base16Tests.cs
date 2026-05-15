// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base16" />. Partial files split coverage by member or subject contract per the
/// repository test convention (see <c>CLAUDE.md</c>).
/// </summary>
[TestClass]
public sealed partial class Base16Tests
{
    /// <summary>
    /// A canonical four-byte input used as the reference vector across encode/decode round-trip tests.
    /// </summary>
    private static readonly byte[] CanonicalBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

    /// <summary>
    /// The lower case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexLower = "deadbeef";

    /// <summary>
    /// The upper case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexUpper = "DEADBEEF";
}
