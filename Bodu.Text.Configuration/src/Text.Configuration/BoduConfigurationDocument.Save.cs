// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocument.Save.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationDocument
{
    /// <summary>
    /// Writes the document to the supplied file, replacing any existing content.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The write options to apply, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentException"><paramref name="path" /> is <see langword="null" />, empty, or
    /// whitespace.</exception>
    public void Save(string path, BoduConfigurationWriteOptions? options = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        BoduConfigurationWriteOptions effective = options ?? BoduConfigurationWriteOptions.Bodu;
        using FileStream stream = File.Create(path);
        using StreamWriter writer = new(stream, effective.Encoding);
        new BoduConfigurationWriter(effective).Write(this, writer);
    }

    /// <summary>
    /// Writes the document to the supplied stream.
    /// </summary>
    /// <param name="stream">The destination stream. Must support writing.</param>
    /// <param name="options">The write options to apply, or <see langword="null" /> for the defaults.</param>
    /// <param name="leaveOpen">When <see langword="true" />, the stream remains open after writing.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream" /> does not support writing.</exception>
    public void Save(Stream stream, BoduConfigurationWriteOptions? options = null, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        if (!stream.CanWrite)
            throw new ArgumentException("Stream does not support writing.", nameof(stream));

        BoduConfigurationWriteOptions effective = options ?? BoduConfigurationWriteOptions.Bodu;
        using StreamWriter writer = new(stream, effective.Encoding, bufferSize: 1024, leaveOpen: leaveOpen);
        new BoduConfigurationWriter(effective).Write(this, writer);
    }

    /// <summary>
    /// Writes the document to the supplied text writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">The write options to apply, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer" /> is <see langword="null" />.</exception>
    public void Save(TextWriter writer, BoduConfigurationWriteOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(writer);
        new BoduConfigurationWriter(options ?? BoduConfigurationWriteOptions.Bodu).Write(this, writer);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        using StringWriter sw = new();
        this.Save(sw, BoduConfigurationWriteOptions.Bodu);
        return sw.ToString();
    }
}
