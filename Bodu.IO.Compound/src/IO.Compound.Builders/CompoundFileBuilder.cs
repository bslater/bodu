// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Authors an OLE2 / Compound File Binary container by accumulating a mutable storage/stream tree and serializing it on
/// demand.
/// </summary>
/// <remarks>
/// <para>
/// Populate <see cref="Root" /> with child storages and streams using the <see cref="CompoundStorageBuilder" />
/// authoring API, then emit the container with <see cref="WriteTo(Stream)" />, <see cref="Save(string)" />, or
/// <see cref="ToArray" />.
/// </para>
/// <para>
/// Because the compound-file layout is a whole-file property — the header, file-allocation table, mini-FAT, and
/// directory all depend on the complete set of streams — the builder serializes the entire tree in a single pass each
/// time an output method is called. The builder holds no destination and is not <see cref="IDisposable" />; stream
/// payloads may be supplied eagerly or as deferred sources read only during serialization, so large streams need not be
/// held in memory.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
///
/// var builder = new CompoundFileBuilder();
/// builder.Root.AddStream("Workbook", workbookBytes);
/// builder.Root.AddStorage("Storage 1").AddStream("Nested", nestedBytes);
/// builder.Save("book.xls");
///]]>
/// </code>
/// </example>
public sealed class CompoundFileBuilder
{
    /// <summary>The options controlling the serialized layout.</summary>
    private readonly CompoundBuildOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileBuilder" /> class with an empty root storage.
    /// </summary>
    /// <param name="options">The options controlling the output layout.</param>
    public CompoundFileBuilder(CompoundBuildOptions options = default)
        : this(CompoundStorageBuilder.CreateRoot(), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileBuilder" /> class over an existing root storage.
    /// </summary>
    /// <param name="root">The root storage the builder serializes.</param>
    /// <param name="options">The options controlling the output layout.</param>
    private CompoundFileBuilder(CompoundStorageBuilder root, CompoundBuildOptions options)
    {
        Root = root;
        _options = options;
    }

    /// <summary>
    /// Gets the root storage that accumulates the authored hierarchy.
    /// </summary>
    /// <returns>The mutable root <see cref="CompoundStorageBuilder" /> to populate before serializing.</returns>
    public CompoundStorageBuilder Root { get; }

    /// <summary>
    /// Creates a builder whose root storage mirrors the contents of a compound file read from a stream.
    /// </summary>
    /// <param name="source">The stream containing the compound file; read from its current position to the end.</param>
    /// <param name="options">The options controlling the output layout when the builder is later serialized.</param>
    /// <returns>A <see cref="CompoundFileBuilder" /> whose <see cref="Root" /> mirrors the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream is not a well-formed compound file.
    /// </exception>
    public static CompoundFileBuilder Load(Stream source, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(source);

        using CompoundFile file = CompoundFile.Open(source, CompoundFileMode.Read, leaveOpen: true);
        return FromFile(file, lazy: false, options);
    }

    /// <summary>
    /// Creates a builder whose root storage mirrors the contents of an open compound file.
    /// </summary>
    /// <param name="file">The compound file to copy.</param>
    /// <param name="lazy">
    /// <see langword="false" /> (the default) to copy every stream payload into memory, producing a fully detached
    /// builder; <see langword="true" /> to build deferred stream nodes that read their payloads on demand from
    /// <paramref name="file" />. When <see langword="true" /> the file must remain open for as long as the builder (or
    /// any clone of its nodes) is read or serialized.
    /// </param>
    /// <param name="options">The options controlling the output layout when the builder is later serialized.</param>
    /// <returns>A <see cref="CompoundFileBuilder" /> whose <see cref="Root" /> mirrors the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when a stream's sector chain is malformed.</exception>
    public static CompoundFileBuilder FromFile(CompoundFile file, bool lazy = false, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(file);

        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CopyMetadata(root, file.RootStorage.Stat);
        Populate(root, file.RootStorage, lazy);
        return new CompoundFileBuilder(root, options);
    }

    /// <summary>
    /// Serializes the authored tree to the supplied stream.
    /// </summary>
    /// <param name="destination">The stream to write the compound file to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void WriteTo(Stream destination)
    {
        ThrowHelper.ThrowIfNull(destination);

        CompoundContainerLayout.WriteTo(destination, Root, _options);
    }

    /// <summary>
    /// Serializes the authored tree to the supplied buffer writer.
    /// </summary>
    /// <param name="output">The buffer writer to write the compound file to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void WriteTo(IBufferWriter<byte> output)
    {
        ThrowHelper.ThrowIfNull(output);

        output.Write(CompoundContainerLayout.Write(Root, _options));
    }

    /// <summary>
    /// Serializes the authored tree to a new file at the supplied path, overwriting any existing file.
    /// </summary>
    /// <param name="path">The path of the file to create.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void Save(string path)
    {
        ThrowHelper.ThrowIfNull(path);

        using FileStream stream = File.Create(path);
        WriteTo(stream);
    }

    /// <summary>
    /// Serializes the authored tree to a compound-file byte array.
    /// </summary>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public byte[] ToArray() =>
        CompoundContainerLayout.Write(Root, _options);

    /// <summary>
    /// Recursively copies the streams and child storages of a read-only storage into a mutable storage node.
    /// </summary>
    /// <param name="target">The mutable storage receiving the children.</param>
    /// <param name="source">The read-only storage to copy.</param>
    /// <param name="lazy">Whether stream nodes defer reading their payloads from the source file.</param>
    private static void Populate(CompoundStorageBuilder target, CompoundStorage source, bool lazy)
    {
        foreach (CompoundStreamEntry entry in source.EnumerateStreams())
        {
            CompoundStreamEntry stream = entry;
            CompoundStreamBuilder node = lazy
                ? target.AddStream(stream.Name, () => stream.Open(), stream.Length)
                : target.AddStream(stream.Name, stream.ReadAllBytes());
            CopyMetadata(node, stream.Stat);
        }

        foreach (CompoundStorage child in source.EnumerateStorages())
        {
            CompoundStorageBuilder node = target.AddStorage(child.Name);
            CopyMetadata(node, child.Stat);
            Populate(node, child, lazy);
        }
    }

    /// <summary>
    /// Copies the metadata of a directory entry onto a mutable node.
    /// </summary>
    /// <param name="node">The node receiving the metadata.</param>
    /// <param name="stat">The source metadata snapshot.</param>
    private static void CopyMetadata(CompoundEntryBuilder node, CompoundEntryInfo stat)
    {
        node.ClassId = stat.ClassId;
        node.StateBits = stat.StateBits;
        node.CreationTime = stat.CreationTime;
        node.ModifiedTime = stat.LastModifiedTime;
    }
}
