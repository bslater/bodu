// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerLayout.Async.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound.Internal;

internal static partial class CompoundContainerLayout
{
    /// <summary>
    /// Serializes the supplied root storage to a destination stream asynchronously, emitting one sector at a time.
    /// </summary>
    /// <param name="destination">The stream that receives the compound-file content.</param>
    /// <param name="root">The root storage to serialize.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the container has been written.</returns>
    /// <remarks>
    /// The layout is computed by the same <see cref="PreparePlan" /> the synchronous <see cref="WriteTo" /> uses, and
    /// the emission mirrors it exactly; only the byte-moving calls are asynchronous. Cancellation may leave the
    /// destination partially written.
    /// </remarks>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> is canceled.</exception>
    internal static async Task WriteToAsync(Stream destination, CompoundStorageBuilder root, CompoundBuildOptions options, CancellationToken cancellationToken)
    {
        EmissionPlan plan = PreparePlan(root, options);
        var writer = new SectorWriter(destination, plan.SectorSize);

        await EmitHeaderAsync(writer, plan, cancellationToken).ConfigureAwait(false);
        await EmitDirectoryAsync(writer, plan.Entries, cancellationToken).ConfigureAwait(false);
        if (plan.MiniFatSectors > 0)
            await EmitMiniFatAsync(writer, plan, cancellationToken).ConfigureAwait(false);

        if (plan.MiniStreamSectors > 0)
        {
            await EmitMiniStreamAsync(writer, plan.Entries, cancellationToken).ConfigureAwait(false);
            await writer.PadToSectorAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsRegularStream)
            {
                await CopyContentAsync(writer, entry, cancellationToken).ConfigureAwait(false);
                await writer.WriteZerosAsync((CeilDiv(entry.Size, plan.SectorSize) * plan.SectorSize) - entry.Size, cancellationToken).ConfigureAwait(false);
            }
        }

        await EmitFatAsync(writer, plan, cancellationToken).ConfigureAwait(false);

        if (plan.DifatSectors > 0)
            await EmitDifatAsync(writer, plan, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously emits the header sector: the 512-byte header followed by zero padding to the sector boundary.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the header has been emitted.</returns>
    private static async Task EmitHeaderAsync(SectorWriter writer, EmissionPlan plan, CancellationToken cancellationToken)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            BuildHeader(plan, rented.AsSpan(0, 512));
            await writer.WriteBytesAsync(rented.AsMemory(0, 512), cancellationToken).ConfigureAwait(false);
            await writer.PadToSectorAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Asynchronously emits the directory sectors, encoding each entry and padding the final sector with zero.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries, in stream-identifier order.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the directory has been emitted.</returns>
    private static async Task EmitDirectoryAsync(SectorWriter writer, List<Entry> entries, CancellationToken cancellationToken)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(DirectoryEntrySize);
        try
        {
            foreach (Entry entry in entries)
            {
                Array.Clear(rented, 0, DirectoryEntrySize);
                entry.Encode(rented.AsSpan(0, DirectoryEntrySize));
                await writer.WriteBytesAsync(rented.AsMemory(0, DirectoryEntrySize), cancellationToken).ConfigureAwait(false);
            }

            await writer.PadToSectorAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Asynchronously emits the mini-FAT sectors, chaining each mini-stream member and padding with free markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the mini-FAT has been emitted.</returns>
    private static async Task EmitMiniFatAsync(SectorWriter writer, EmissionPlan plan, CancellationToken cancellationToken)
    {
        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsMiniStream)
                await WriteChainAsync(writer, entry.StartSector, CeilDiv(entry.Size, MiniSectorSize), cancellationToken).ConfigureAwait(false);
        }

        await WriteFreeAsync(writer, (plan.MiniFatSectors * plan.EntriesPerSector) - plan.TotalMiniSectors, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously emits the mini-stream content, padding each member to a mini-sector boundary.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the mini stream has been emitted.</returns>
    private static async Task EmitMiniStreamAsync(SectorWriter writer, List<Entry> entries, CancellationToken cancellationToken)
    {
        foreach (Entry entry in entries)
        {
            if (!entry.IsMiniStream)
                continue;

            await CopyContentAsync(writer, entry, cancellationToken).ConfigureAwait(false);
            await writer.WriteZerosAsync((CeilDiv(entry.Size, MiniSectorSize) * MiniSectorSize) - entry.Size, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously emits the FAT sectors, chaining every sector run, marking the FAT and DIFAT sectors, and padding
    /// with free markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the FAT has been emitted.</returns>
    private static async Task EmitFatAsync(SectorWriter writer, EmissionPlan plan, CancellationToken cancellationToken)
    {
        await WriteChainAsync(writer, (uint)plan.DirectoryStart, plan.DirectorySectors, cancellationToken).ConfigureAwait(false);
        if (plan.MiniFatSectors > 0)
            await WriteChainAsync(writer, plan.MiniFatStart, plan.MiniFatSectors, cancellationToken).ConfigureAwait(false);

        if (plan.MiniStreamSectors > 0)
            await WriteChainAsync(writer, plan.MiniStreamStart, plan.MiniStreamSectors, cancellationToken).ConfigureAwait(false);

        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsRegularStream)
                await WriteChainAsync(writer, entry.StartSector, CeilDiv(entry.Size, plan.SectorSize), cancellationToken).ConfigureAwait(false);
        }

        for (long i = 0; i < plan.FatSectors; i++)
            await writer.WriteUInt32Async(CfbHeader.FatSector, cancellationToken).ConfigureAwait(false);

        for (long i = 0; i < plan.DifatSectors; i++)
            await writer.WriteUInt32Async(CfbHeader.DifatSector, cancellationToken).ConfigureAwait(false);

        long totalSectors = plan.DataSectorCount + plan.FatSectors + plan.DifatSectors;
        await WriteFreeAsync(writer, (plan.FatSectors * plan.EntriesPerSector) - totalSectors, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously emits the extended DIFAT sectors, each carrying FAT-sector indices and a chain pointer.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the DIFAT has been emitted.</returns>
    private static async Task EmitDifatAsync(SectorWriter writer, EmissionPlan plan, CancellationToken cancellationToken)
    {
        int slotsPerSector = plan.EntriesPerSector - 1;
        long fatIndex = CfbHeader.HeaderDifatCount;
        for (long s = 0; s < plan.DifatSectors; s++)
        {
            for (int j = 0; j < slotsPerSector; j++)
            {
                await writer.WriteUInt32Async(fatIndex < plan.FatSectors ? (uint)(plan.FatStart + fatIndex) : CfbHeader.FreeSector, cancellationToken).ConfigureAwait(false);
                fatIndex++;
            }

            await writer.WriteUInt32Async(s == plan.DifatSectors - 1 ? CfbHeader.EndOfChain : (uint)(plan.DifatStart + s + 1), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously writes a sequential sector chain to the FAT, terminating with the end-of-chain marker.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="start">The first sector of the run.</param>
    /// <param name="count">The number of sectors in the run.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the chain has been written.</returns>
    private static async Task WriteChainAsync(SectorWriter writer, uint start, long count, CancellationToken cancellationToken)
    {
        for (long i = 0; i < count; i++)
            await writer.WriteUInt32Async(i == count - 1 ? CfbHeader.EndOfChain : (uint)(start + i + 1), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a run of free-sector markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="count">The number of markers to write.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the markers have been written.</returns>
    private static async Task WriteFreeAsync(SectorWriter writer, long count, CancellationToken cancellationToken)
    {
        for (long i = 0; i < count; i++)
            await writer.WriteUInt32Async(CfbHeader.FreeSector, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously copies a stream entry's payload to the writer, from memory or a deferred source.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entry">The stream entry to copy.</param>
    /// <param name="cancellationToken">A token that cancels the copy.</param>
    /// <returns>A task that completes when the payload has been copied.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when a deferred source is shorter than its declared length.
    /// </exception>
    private static async Task CopyContentAsync(SectorWriter writer, Entry entry, CancellationToken cancellationToken)
    {
        CompoundStreamBuilder node = entry.StreamNode!;
        if (!node.IsDeferred)
        {
            await writer.WriteBytesAsync(node.Content, cancellationToken).ConfigureAwait(false);
            return;
        }

        long remaining = entry.Size;
        using Stream source = node.OpenContentStream();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            while (remaining > 0)
            {
                // Observe cancellation between chunks even when the underlying source ignores the token.
                cancellationToken.ThrowIfCancellationRequested();

                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = await source.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    throw new CompoundFileSerializationException(
                        string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundBuilderStreamLength, node.Name, entry.Size));
                }

                await writer.WriteBytesAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private sealed partial class SectorWriter
    {
        /// <summary>
        /// Asynchronously writes raw bytes, flushing whole sectors as they fill.
        /// </summary>
        /// <param name="data">The bytes to write.</param>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the bytes have been buffered and any full sectors flushed.</returns>
        public async ValueTask WriteBytesAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            while (!data.IsEmpty)
            {
                int n = Math.Min(_buffer.Length - _position, data.Length);
                data.Span.Slice(0, n).CopyTo(_buffer.AsSpan(_position));
                _position += n;
                data = data.Slice(n);
                if (_position == _buffer.Length)
                    await FlushSectorAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously writes a little-endian 32-bit value. The caller must keep the position four-byte aligned.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the value has been buffered and any full sector flushed.</returns>
        public async ValueTask WriteUInt32Async(uint value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
            if (_position == _buffer.Length)
                await FlushSectorAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes a run of zero bytes, flushing whole sectors as they fill.
        /// </summary>
        /// <param name="count">The number of zero bytes to write.</param>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the zeros have been buffered and any full sectors flushed.</returns>
        public async ValueTask WriteZerosAsync(long count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                int n = (int)Math.Min(_buffer.Length - _position, count);
                _buffer.AsSpan(_position, n).Clear();
                _position += n;
                count -= n;
                if (_position == _buffer.Length)
                    await FlushSectorAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously zero-fills the remainder of the active sector and flushes it, leaving the writer
        /// sector-aligned.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the sector has been flushed.</returns>
        public async ValueTask PadToSectorAsync(CancellationToken cancellationToken)
        {
            if (_position > 0)
            {
                _buffer.AsSpan(_position).Clear();
                await FlushSectorAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously writes the active sector buffer to the destination and resets the position.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the sector has been written.</returns>
        private async ValueTask FlushSectorAsync(CancellationToken cancellationToken)
        {
            await _destination.WriteAsync(_buffer.AsMemory(0, _buffer.Length), cancellationToken).ConfigureAwait(false);
            _position = 0;
        }
    }
}
