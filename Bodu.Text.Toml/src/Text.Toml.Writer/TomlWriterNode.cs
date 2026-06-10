// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Represents a node in the in-memory value tree that <see cref="Utf8TomlWriter" /> assembles from its forward
/// <c>Write*</c> calls before serializing the completed tree to canonical TOML.
/// </summary>
/// <remarks>
/// TOML's surface layout is a whole-document property — whether a table becomes a <c>[header]</c> block or an inline
/// <c>{ … }</c> depends on where it sits — so the writer cannot emit incrementally. It buffers each value into one of
/// the three concrete node kinds (<see cref="TomlTableWriterNode" />, <see cref="TomlArrayWriterNode" />, or
/// <see cref="TomlScalarWriterNode" />) and renders the root table once it is closed.
/// </remarks>
internal abstract class TomlWriterNode
{
}
