// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.Binary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>
    /// Compiles the document to a sealed binary rule pack on disk.
    /// </summary>
    /// <param name="path">The destination file path, conventionally with the <c>.bcal</c> extension.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The document produced one or more error diagnostics.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The document is materialized through <see cref="Build()" /> first, so only content that passed the canonical
    /// loader's validation is ever encoded; the pack then loads via
    /// <see cref="NotableDateResourceLoader.LoadBinary(Stream)" /> without re-parsing or re-validating.
    /// </para>
    /// </remarks>
    public void SaveBinary(string path) =>
        SaveBinary(path, null);

    /// <summary>
    /// Compiles the document to a sealed binary rule pack on disk, resolving imports through the supplied resolver.
    /// </summary>
    /// <param name="path">The destination file path, conventionally with the <c>.bcal</c> extension.</param>
    /// <param name="importResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> to resolve no imports.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The document produced one or more error diagnostics.
    /// </exception>
    public void SaveBinary(string path, Func<string, string?>? importResolver)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.Create(path);
        SaveBinary(stream, importResolver);
    }

    /// <summary>
    /// Compiles the document to a sealed binary rule pack written to a stream.
    /// </summary>
    /// <param name="stream">The writable stream receiving the pack.</param>
    /// <param name="importResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> to resolve no imports.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The document produced one or more error diagnostics.
    /// </exception>
    public void SaveBinary(Stream stream, Func<string, string?>? importResolver = null)
    {
        ThrowHelper.ThrowIfNull(stream);

        NotableDateResource resource = Build(importResolver);
        NotableDateBinaryResource.Write(resource, stream);
    }
}
