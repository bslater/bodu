// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.ContainerPatcher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Applies the <c>@cfb=</c> container patches to a written compound file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every location is resolved through the file's own header, FAT and directory (<see cref="ContainerLayout" />), so
    /// a patch does not depend on where the writer placed the sectors. <c>corpus/biff/README.md</c> lists the patches:
    /// <c>hdr</c>, <c>dir</c>, <c>fat</c>, <c>size</c>, <c>cut</c>, <c>add</c>, <c>move</c>, <c>difat</c>,
    /// <c>difmove</c>, <c>difnext</c> and <c>difslot</c>.
    /// </para>
    /// <para>
    /// A patch that appends a sector first pads the file to a whole number of sectors and then adds one sector, so the
    /// new sector's number is the file's sector count less one (the header occupies the first sector's place).
    /// </para>
    /// </remarks>
    private static class ContainerPatcher
    {
        /// <summary>
        /// Checks a patch's shape without applying it.
        /// </summary>
        /// <param name="patch">The patch, without its <c>@cfb=</c> prefix.</param>
        /// <exception cref="FormatException">The patch is unknown, or has the wrong number of fields.</exception>
        public static void Check(string patch)
        {
            string[] fields = patch.Split(':');
            bool known = fields[0] switch
            {
                "hdr" or "move" or "difnext" => fields.Length == 3,
                "dir" or "fat" or "difslot" => fields.Length == 4,
                "size" or "cut" or "add" or "difat" or "difmove" => fields.Length == 2,
                _ => false,
            };

            if (!known)
                throw new FormatException($"The container patch '{patch}' is not one the generators know.");
        }

        /// <summary>
        /// Applies one patch.
        /// </summary>
        /// <param name="file">The compound file; patches that keep its length change it in place.</param>
        /// <param name="patch">The patch, without its <c>@cfb=</c> prefix.</param>
        /// <returns>The patched file.</returns>
        /// <exception cref="FormatException">The patch names something the file does not have.</exception>
        public static byte[] Apply(byte[] file, string patch)
        {
            string[] fields = patch.Split(':');
            switch (fields[0])
            {
                case "hdr":
                    Convert.FromHexString(fields[2]).CopyTo(file, RecordBuilder.ParseInt32(fields[1]));
                    return file;

                case "size":
                {
                    var resized = new byte[RecordBuilder.ParseInt32(fields[1])];
                    Array.Copy(file, resized, Math.Min(resized.Length, file.Length));
                    return resized;
                }

                case "cut":
                    return file[..^RecordBuilder.ParseInt32(fields[1])];

                case "add":
                {
                    var extended = new byte[file.Length + RecordBuilder.ParseInt32(fields[1])];
                    file.CopyTo(extended, 0);
                    return extended;
                }

                case "difat":
                    return AppendDifatSector(file, fields[1]);

                case "difmove":
                    return MoveDifatSector(file, fields[1]);

                case "difnext":
                {
                    var layout = new ContainerLayout(file);
                    uint sector = layout.GetDifatSector(fields[1]);
                    uint value = fields[2] == "self" ? sector : layout.ResolveValue(fields[2]);
                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(layout.GetSectorOffset(sector) + layout.SectorSize - 4), value);
                    return file;
                }

                case "difslot":
                {
                    var layout = new ContainerLayout(file);
                    uint sector = layout.GetDifatSector(fields[1]);
                    int slot = RecordBuilder.ParseInt32(fields[2]);
                    if (slot < 0 || slot >= (layout.SectorSize / 4) - 1)
                        throw new FormatException($"A DIFAT sector has no slot {fields[2]}.");

                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(layout.GetSectorOffset(sector) + (slot * 4)), layout.ResolveValue(fields[3]));
                    return file;
                }

                case "dir":
                    SetDirectoryField(file, new ContainerLayout(file), fields[1], fields[2], fields[3]);
                    return file;

                case "move":
                    return MoveChainSector(file, fields[1], fields[2]);

                case "fat":
                {
                    var layout = new ContainerLayout(file);
                    uint start = fields[1] == "directory" ? layout.FirstDirectorySector : layout.GetStartSector(layout.FindEntry(fields[1]));
                    List<uint> chain = layout.GetChain(start);
                    int k = RecordBuilder.ParseInt32(fields[2]);
                    if (k < 0)
                        k += chain.Count;

                    if (k < 0 || k >= chain.Count)
                        throw new FormatException($"The chain of '{fields[1]}' has no sector {fields[2]}.");

                    layout.WriteFat(chain[k], layout.ResolveValue(fields[3]));
                    return file;
                }

                default:
                    throw new FormatException($"The container patch '{patch}' is not one the generators know.");
            }
        }

        /// <summary>
        /// Sets one field of a directory entry.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="layout">The file's layout.</param>
        /// <param name="entryName">The entry: <c>root</c>, <c>#N</c> or a name.</param>
        /// <param name="field">
        /// The field: <c>type</c>, <c>color</c>, <c>left</c>, <c>right</c>, <c>child</c>, <c>start</c>, <c>size</c>,
        /// <c>namelen</c>, <c>ctime</c> or <c>mtime</c>.
        /// </param>
        /// <param name="value">
        /// The value: a sector value for the one-, two- and four-byte fields, a number for the others.
        /// </param>
        /// <exception cref="FormatException">
        /// The entry or the field does not exist, or the value is malformed.
        /// </exception>
        private static void SetDirectoryField(byte[] file, ContainerLayout layout, string entryName, string field, string value)
        {
            int at = layout.GetEntryOffset(layout.FindEntry(entryName));
            switch (field)
            {
                case "type":
                    file[at + 66] = (byte)layout.ResolveValue(value);
                    break;

                case "color":
                    file[at + 67] = (byte)layout.ResolveValue(value);
                    break;

                case "left":
                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 68), layout.ResolveValue(value));
                    break;

                case "right":
                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 72), layout.ResolveValue(value));
                    break;

                case "child":
                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 76), layout.ResolveValue(value));
                    break;

                case "start":
                    BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 116), layout.ResolveValue(value));
                    break;

                case "size":
                    BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(at + 120), (ulong)RecordBuilder.ParseInt64(value));
                    break;

                case "ctime":
                    BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(at + 100), RecordBuilder.ParseInt64(value));
                    break;

                case "mtime":
                    BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(at + 108), RecordBuilder.ParseInt64(value));
                    break;

                case "namelen":
                    BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(at + 64), (ushort)layout.ResolveValue(value));
                    break;

                default:
                    throw new FormatException($"The directory field '{field}' is not one the patches know.");
            }
        }

        /// <summary>
        /// Appends one DIFAT sector that names no FAT sector, and links it from the header.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="next">The sector value the new sector's next-sector field holds.</param>
        /// <returns>
        /// The longer file: the header names the new sector as its first DIFAT sector, with a count of one, and the FAT
        /// marks it DIFSECT.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="next" /> is malformed.</exception>
        private static byte[] AppendDifatSector(byte[] file, string next)
        {
            byte[] extended = AppendSector(file, out uint sector, out int sectorSize);
            int at = (int)((sector + 1) * sectorSize);
            for (var i = 0; i < (sectorSize / 4) - 1; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(at + (i * 4)), ContainerLayout.FreeSector);

            var layout = new ContainerLayout(extended);
            BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(at + sectorSize - 4), layout.ResolveValue(next));
            BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(0x44), sector);
            BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(0x48), 1);
            layout.WriteFat(sector, ContainerLayout.DifatSectorMarker);
            return extended;
        }

        /// <summary>
        /// Moves one DIFAT sector to a new sector at the end of the file, so the DIFAT sectors are no longer
        /// consecutive.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="index">
        /// The position of the sector in the DIFAT chain, from zero; a negative position counts from the end.
        /// </param>
        /// <returns>
        /// The longer file: the DIFAT chain runs through the copy (from the header when <paramref name="index" /> names
        /// the first sector), the FAT marks the copy DIFSECT and the old sector free.
        /// </returns>
        /// <exception cref="FormatException">The DIFAT chain has no such sector.</exception>
        private static byte[] MoveDifatSector(byte[] file, string index)
        {
            List<uint> difat = new ContainerLayout(file).GetDifatSectors();
            int k = ResolveIndex(index, difat.Count, "the DIFAT chain");
            uint old = difat[k];

            byte[] extended = AppendSector(file, out uint fresh, out int sectorSize);
            Array.Copy(file, (int)((old + 1) * sectorSize), extended, (int)((fresh + 1) * sectorSize), sectorSize);
            if (k == 0)
                BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(0x44), fresh);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan((int)((difat[k - 1] + 1) * sectorSize) + sectorSize - 4), fresh);

            var layout = new ContainerLayout(extended);
            layout.WriteFat(fresh, ContainerLayout.DifatSectorMarker);
            layout.WriteFat(old, ContainerLayout.FreeSector);
            return extended;
        }

        /// <summary>
        /// Moves one sector of an entry's chain to a new sector at the end of the file, so the chain is no longer in
        /// file order.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="entryName">The entry whose chain holds the sector.</param>
        /// <param name="index">
        /// The position of the sector in the chain, from zero; a negative position counts from the end.
        /// </param>
        /// <returns>
        /// The longer file: the chain runs through the copy (from the entry's start sector when
        /// <paramref name="index" /> names the first sector), and the FAT marks the old sector free.
        /// </returns>
        /// <exception cref="FormatException">The entry does not exist, or its chain has no such sector.</exception>
        private static byte[] MoveChainSector(byte[] file, string entryName, string index)
        {
            var layout = new ContainerLayout(file);
            int entry = layout.FindEntry(entryName);
            List<uint> chain = layout.GetChain(layout.GetStartSector(entry));
            int k = ResolveIndex(index, chain.Count, $"the chain of '{entryName}'");
            uint old = chain[k];

            byte[] extended = AppendSector(file, out uint fresh, out int sectorSize);
            Array.Copy(file, (int)((old + 1) * sectorSize), extended, (int)((fresh + 1) * sectorSize), sectorSize);
            var moved = new ContainerLayout(extended);
            moved.WriteFat(fresh, moved.ReadFat(old));
            moved.WriteFat(old, ContainerLayout.FreeSector);
            if (k == 0)
                BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(moved.GetEntryOffset(entry) + 116), fresh);
            else
                moved.WriteFat(chain[k - 1], fresh);

            return extended;
        }

        /// <summary>
        /// Pads a file to a whole number of sectors and appends one zeroed sector.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="sector">When the method returns, the number of the appended sector.</param>
        /// <param name="sectorSize">When the method returns, the file's sector size.</param>
        /// <returns>The longer file.</returns>
        private static byte[] AppendSector(byte[] file, out uint sector, out int sectorSize)
        {
            sectorSize = 1 << BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(0x1E));
            int whole = (file.Length + sectorSize - 1) / sectorSize * sectorSize;
            var extended = new byte[whole + sectorSize];
            file.CopyTo(extended, 0);
            sector = (uint)((whole / sectorSize) - 1);
            return extended;
        }

        /// <summary>
        /// Resolves a position in a sequence, counting a negative position from the end.
        /// </summary>
        /// <param name="text">The position.</param>
        /// <param name="count">The sequence length.</param>
        /// <param name="what">The sequence, for the exception message.</param>
        /// <returns>The position from zero.</returns>
        /// <exception cref="FormatException">The sequence has no such position.</exception>
        private static int ResolveIndex(string text, int count, string what)
        {
            int k = RecordBuilder.ParseInt32(text);
            if (k < 0)
                k += count;

            return k >= 0 && k < count ? k : throw new FormatException($"{what} has no sector {text}.");
        }
    }
}
