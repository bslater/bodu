// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.WriteOperation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one writer call of a write-notation transcript.
    /// </summary>
    /// <param name="Token">The call: <c>[</c>, <c>]</c>, <c>{</c>, <c>}</c>, <c>k</c>, <c>s</c> or <c>i</c>.</param>
    /// <param name="Bytes">The key, the byte string, or the integer's digits; empty for a container token.</param>
    private sealed record WriteOperation(string Token, byte[] Bytes);
}
