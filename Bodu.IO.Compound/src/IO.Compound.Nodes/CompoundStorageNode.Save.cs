// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageNode.Save.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Compound.Writer;

namespace Bodu.IO.Compound.Nodes;

public sealed partial class CompoundStorageNode
{
    /// <summary>
    /// Serializes this root storage and its descendants to a compound file written to the supplied stream.
    /// </summary>
    /// <param name="destination">The stream to write the compound file to.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when this node is not a root storage, or the model cannot be represented.
    /// </exception>
    public void Save(Stream destination, CompoundWriterOptions options = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        if (Parent is not null)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Op_Invalid_CompoundWriterSaveNotRoot);

        CompoundContainerLayout.WriteTo(destination, this, options);
    }

    /// <summary>
    /// Serializes this root storage and its descendants to a compound file written to the supplied buffer writer.
    /// </summary>
    /// <param name="output">The buffer writer to write the compound file to.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when this node is not a root storage, or the model cannot be represented.
    /// </exception>
    public void WriteTo(IBufferWriter<byte> output, CompoundWriterOptions options = default)
    {
        ThrowHelper.ThrowIfNull(output);

        output.Write(SerializeRoot(options));
    }

    /// <summary>
    /// Serializes this root storage and its descendants to a compound-file byte array.
    /// </summary>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when this node is not a root storage, or the model cannot be represented.
    /// </exception>
    public byte[] ToArray(CompoundWriterOptions options = default) =>
        SerializeRoot(options);

    /// <summary>
    /// Validates that this node is a root storage and serializes the container.
    /// </summary>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when this node is not a root storage.</exception>
    private byte[] SerializeRoot(CompoundWriterOptions options)
    {
        if (Parent is not null)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Op_Invalid_CompoundWriterSaveNotRoot);

        return CompoundContainerLayout.Write(this, options);
    }
}
