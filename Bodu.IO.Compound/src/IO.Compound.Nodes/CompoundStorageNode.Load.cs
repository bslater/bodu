// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageNode.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Nodes;

public sealed partial class CompoundStorageNode
{
    /// <summary>
    /// Loads a compound file from a stream into a detached, mutable object model.
    /// </summary>
    /// <param name="source">The stream containing the compound file; read from its current position to the end.</param>
    /// <returns>A root <see cref="CompoundStorageNode" /> mirroring the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream is not a well-formed compound file.
    /// </exception>
    public static CompoundStorageNode Load(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);

        using CompoundFile file = CompoundFile.Open(source, CompoundFileMode.Read, leaveOpen: true);
        return FromFile(file);
    }

    /// <summary>
    /// Builds a mutable object model from an open compound file.
    /// </summary>
    /// <param name="file">The compound file to copy.</param>
    /// <param name="lazy">
    /// <see langword="false" /> (the default) to copy every stream payload into memory, producing a fully detached
    /// model; <see langword="true" /> to build deferred stream nodes that read their payloads on demand from
    /// <paramref name="file" />. When <see langword="true" /> the file must remain open for as long as the returned
    /// nodes (or any clones) are read or serialized.
    /// </param>
    /// <returns>A root <see cref="CompoundStorageNode" /> mirroring the file's contents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when a stream's sector chain is malformed.</exception>
    public static CompoundStorageNode FromFile(CompoundFile file, bool lazy = false)
    {
        ThrowHelper.ThrowIfNull(file);

        CompoundStorageNode root = CreateRoot();
        CopyMetadata(root, file.RootStorage.Stat);
        Populate(root, file.RootStorage, lazy);
        return root;
    }

    /// <summary>
    /// Recursively copies the streams and child storages of a read-only storage into a mutable storage node.
    /// </summary>
    /// <param name="target">The mutable storage receiving the children.</param>
    /// <param name="source">The read-only storage to copy.</param>
    /// <param name="lazy">Whether stream nodes defer reading their payloads from the source file.</param>
    private static void Populate(CompoundStorageNode target, CompoundStorage source, bool lazy)
    {
        foreach (CompoundStreamEntry entry in source.EnumerateStreams())
        {
            CompoundStreamEntry stream = entry;
            CompoundStreamNode node = lazy
                ? target.AddStream(stream.Name, () => stream.Open(), stream.Length)
                : target.AddStream(stream.Name, stream.ReadAllBytes());
            CopyMetadata(node, stream.Stat);
        }

        foreach (CompoundStorage child in source.EnumerateStorages())
        {
            CompoundStorageNode node = target.AddStorage(child.Name);
            CopyMetadata(node, child.Stat);
            Populate(node, child, lazy);
        }
    }

    /// <summary>
    /// Copies the metadata of a directory entry onto a mutable node.
    /// </summary>
    /// <param name="node">The node receiving the metadata.</param>
    /// <param name="stat">The source metadata snapshot.</param>
    private static void CopyMetadata(CompoundNode node, CompoundEntryInfo stat)
    {
        node.ClassId = stat.ClassId;
        node.StateBits = stat.StateBits;
        node.CreationTime = stat.CreationTime;
        node.ModifiedTime = stat.LastModifiedTime;
    }
}
