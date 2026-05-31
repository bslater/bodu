// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="Bencode" />. Partial files split coverage by member or subject contract per
/// the repository test convention (see <c>CLAUDE.md</c>).
/// </summary>
[TestClass]
public sealed partial class BencodeTests
{

    /// <summary>
    /// Canonical encoded form of an empty dictionary (<c>de</c>).
    /// </summary>
    private static readonly byte[] CanonicalEmptyDictBytes = System.Text.Encoding.ASCII.GetBytes("de");

    /// <summary>
    /// Canonical encoded form of an empty list (<c>le</c>).
    /// </summary>
    private static readonly byte[] CanonicalEmptyListBytes = System.Text.Encoding.ASCII.GetBytes("le");

    /// <summary>
    /// Canonical encoded form of <c>i42e</c>.
    /// </summary>
    private static readonly byte[] CanonicalIntegerBytes = System.Text.Encoding.ASCII.GetBytes("i42e");
    /// <summary>
    /// Canonical encoded form of <c>4:spam</c> — the BEP 3 example string used across happy-path tests.
    /// </summary>
    private static readonly byte[] CanonicalSpamBytes = System.Text.Encoding.ASCII.GetBytes("4:spam");

    /// <summary>
    /// Converts an ASCII string to its byte representation, a common convenience across the test partials.
    /// </summary>
    /// <param name="ascii">The ASCII text to encode.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Bytes(string ascii) =>
        System.Text.Encoding.ASCII.GetBytes(ascii);

}
