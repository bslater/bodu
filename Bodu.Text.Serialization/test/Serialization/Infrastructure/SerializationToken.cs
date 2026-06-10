// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// Represents a single format-neutral token captured by the in-memory test reader and writer, pairing a
/// <see cref="SerializationValueKind" /> with the value it carries.
/// </summary>
/// <param name="Kind">The token kind.</param>
/// <param name="Value">The value the token carries, or <see langword="null" /> for structural tokens.</param>
internal sealed record SerializationToken(SerializationValueKind Kind, object? Value);
