// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="QuotedPrintable" />. Partial files split coverage by member or subject contract.
/// </summary>
[TestClass]
public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Returns the ASCII bytes for <paramref name="value" />.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    /// <summary>
    /// Returns text-mode encoding options with the supplied newline.
    /// </summary>
    /// <param name="newLine">The newline sequence.</param>
    /// <returns>The text-mode options.</returns>
    private static QuotedPrintableEncodingOptions TextOptions(string newLine = "\r\n") =>
        new(QuotedPrintableEncodingMode.Text, 76, newLine);
}
