// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilder.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound.Builders;

/// <content>
/// Serialization and materialization members that turn a detached <see cref="CompoundStorageBuilder" /> snapshot tree
/// into a compound-file container, and back.
/// </content>
public sealed partial class CompoundStorageBuilder
{
    /// <summary>
    /// Creates a root storage tree that mirrors the contents of a compound file read from a stream.
    /// </summary>
    /// <param name="source">The stream containing the compound file; read from its current position to the end.</param>
    /// <returns>A root <see cref="CompoundStorageBuilder" /> that mirrors the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream is not a well-formed compound file.
    /// </exception>
    public static CompoundStorageBuilder Load(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);

        using CompoundFile file = CompoundFile.Open(source, leaveOpen: true);
        return FromFile(file, lazy: false);
    }

    /// <summary>
    /// Creates a root storage tree that mirrors the contents of an open compound file.
    /// </summary>
    /// <param name="file">The compound file to copy.</param>
    /// <param name="lazy">
    /// <see langword="false" /> (the default) to copy every stream payload into memory, producing a fully detached
    /// tree; <see langword="true" /> to build deferred stream nodes that read their payloads on demand from
    /// <paramref name="file" />. When <see langword="true" /> the file must remain open for as long as the tree (or any
    /// clone of its nodes) is read or serialized.
    /// </param>
    /// <returns>A root <see cref="CompoundStorageBuilder" /> that mirrors the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when a stream's sector chain is malformed.</exception>
    public static CompoundStorageBuilder FromFile(CompoundFile file, bool lazy = false)
    {
        ThrowHelper.ThrowIfNull(file);

        CompoundStorageBuilder root = CreateRoot();
        CopyMetadata(root, file.RootStorage.Stat);
        Populate(root, file.RootStorage, lazy);
        return root;
    }

    /// <summary>
    /// Serializes this storage tree to the supplied stream.
    /// </summary>
    /// <param name="destination">The stream to write the compound file to.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void WriteTo(Stream destination, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(destination);

        CompoundContainerLayout.WriteTo(destination, this, options);
    }

    /// <summary>
    /// Serializes this storage tree to the supplied buffer writer.
    /// </summary>
    /// <param name="output">The buffer writer to write the compound file to.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void WriteTo(IBufferWriter<byte> output, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(output);

        output.Write(CompoundContainerLayout.Write(this, options));
    }

    /// <summary>
    /// Serializes this storage tree to a new file at the supplied path, overwriting any existing file.
    /// </summary>
    /// <param name="path">The path of the file to create.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public void Save(string path, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(path);

        using FileStream stream = File.Create(path);
        WriteTo(stream, options);
    }

    /// <summary>
    /// Serializes this storage tree to a compound-file byte array.
    /// </summary>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the tree cannot be represented.</exception>
    public byte[] ToArray(CompoundBuildOptions options = default) =>
        CompoundContainerLayout.Write(this, options);

    /// <summary>
    /// Recursively copies the streams and child storages of a read-only storage into a mutable storage node.
    /// </summary>
    /// <param name="target">The mutable storage receiving the children.</param>
    /// <param name="source">The read-only storage to copy.</param>
    /// <param name="lazy">Whether stream nodes defer reading their payloads from the source file.</param>
    private static void Populate(CompoundStorageBuilder target, CompoundStorage source, bool lazy)
    {
        foreach (CompoundEntryInfo info in source.EnumerateStreams())
        {
            string name = info.Name;
            CompoundStreamBuilder node;
            if (lazy)
            {
                node = target.AddStream(name, () => source.OpenStream(name), info.Length);
            }
            else
            {
                using CompoundStream stream = source.OpenStream(name);
                node = target.AddStream(name, stream.ReadAllBytes());
            }

            CopyMetadata(node, info);
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
