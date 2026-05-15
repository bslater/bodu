// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base32" />. Partial files split coverage by member or subject contract per the
/// repository test convention.
/// </summary>
[TestClass]
public sealed partial class Base32Tests
{
    /// <summary>
    /// Returns the ASCII bytes for the supplied string, used to build RFC 4648 reference vectors.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);
}
