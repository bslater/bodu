// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base85" />. Partial files split coverage by member or subject contract.
/// </summary>
[TestClass]
public sealed partial class Base85Tests
{

    /// <summary>
    /// Returns the ASCII bytes for <paramref name="value" />.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

}
