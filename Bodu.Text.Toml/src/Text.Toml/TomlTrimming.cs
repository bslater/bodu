// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTrimming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Provides the diagnostic messages attached to the reflection-based serialization surface through
/// <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute" /> and
/// <see cref="System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute" />, so consumers that trim or compile ahead
/// of time receive a clear warning at the call site.
/// </summary>
internal static class TomlTrimming
{
    /// <summary>
    /// The message describing why the serializer is incompatible with trimming: it reflects over the serialized types
    /// and their members, which a trimmer may remove.
    /// </summary>
    internal const string RequiresUnreferencedCodeMessage =
        "TOML serialization and deserialization reflect over the serialized types and their members, which trimming may remove. Ensure the required types and members are preserved.";

    /// <summary>
    /// The message describing why the serializer is incompatible with native AOT: it constructs converters for
    /// collection, dictionary, and nullable types at runtime, which requires dynamic code generation.
    /// </summary>
    internal const string RequiresDynamicCodeMessage =
        "TOML serialization and deserialization construct converters for collection, dictionary, and nullable types at runtime, which native AOT cannot do without runtime code generation.";
}
