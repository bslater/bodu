// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8ContentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="Utf8Content" />, the shared helper through which the delimited-text providers hand a downloaded
/// response to their UTF-8 parsers.
/// </summary>
[TestClass]
public sealed partial class Utf8ContentTests
{
    /// <summary>
    /// Returns the UTF-8 bytes of the text a <see cref="StreamReader" /> decodes some content to: what a parser read
    /// before it read bytes directly.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The UTF-8 bytes of the decoded text.</returns>
    private static byte[] DecodedByStreamReader(byte[] content)
    {
        using StreamReader reader = new(new MemoryStream(content, writable: false));
        return Encoding.UTF8.GetBytes(reader.ReadToEnd());
    }
}
