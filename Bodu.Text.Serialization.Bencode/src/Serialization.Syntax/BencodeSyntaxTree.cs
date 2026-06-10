// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSyntaxTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Provides the entry point for parsing Bencode bytes into a concrete syntax tree.
/// </summary>
/// <remarks>
/// The parsed tree is lossless for valid input: re-encoding it with <see cref="BencodeSyntaxNode.ToByteArray" />
/// reproduces the original bytes exactly, because the Bencode grammar is canonical and carries no insignificant text.
/// </remarks>
public static class BencodeSyntaxTree
{
    /// <summary>
    /// The default maximum container nesting depth.
    /// </summary>
    public const int DefaultMaxDepth = 256;

    /// <summary>
    /// Parses Bencode bytes into a document syntax tree.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="maxDepth">The maximum permitted container nesting depth.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxDepth" /> is not positive.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is not a valid Bencode document.
    /// </exception>
    public static BencodeDocumentSyntax Parse(ReadOnlySpan<byte> data, int maxDepth = DefaultMaxDepth)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maxDepth, 0);

        BencodeParser parser = new(data, maxDepth);
        return parser.Parse();
    }
}
