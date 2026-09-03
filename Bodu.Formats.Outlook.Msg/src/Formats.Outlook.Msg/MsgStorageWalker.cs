// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStorageWalker.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Enumerates the indexed child storages of a message — the <c>__recip_version1.0_#NNNNNNNN</c> and
/// <c>__attach_version1.0_#NNNNNNNN</c> series — in index order.
/// </summary>
/// <remarks>
/// Under <see cref="CompoundValidationLevel.Strict" /> the series must be dense (indexes <c>0..n-1</c> without gaps or
/// duplicates) and its length must match the count the property-stream header declares; under the tolerant levels
/// storages with unparsable suffixes are skipped, a repeated index keeps its first storage in directory order, and
/// the found series is returned as-is.
/// </remarks>
internal static class MsgStorageWalker
{
    /// <summary>
    /// Enumerates the child storages carrying a prefix and an eight-digit hexadecimal index suffix, ordered by index.
    /// </summary>
    /// <param name="parent">The parent storage.</param>
    /// <param name="prefix">The storage-name prefix, including the <c>#</c>.</param>
    /// <param name="declaredCount">The count declared by the property-stream header.</param>
    /// <param name="validationLevel">The validation level governing malformed-structure handling.</param>
    /// <returns>The matching storages in ascending index order.</returns>
    /// <exception cref="OutlookMsgFormatException">
    /// The container is malformed, or — under <see cref="CompoundValidationLevel.Strict" /> — a suffix is malformed,
    /// the series has gaps or duplicates, or its length differs from <paramref name="declaredCount" />.
    /// </exception>
    internal static List<CompoundStorage> EnumerateIndexed(
        CompoundStorage parent,
        string prefix,
        uint declaredCount,
        CompoundValidationLevel validationLevel)
    {
        bool strict = validationLevel == CompoundValidationLevel.Strict;
        var found = new List<(uint Index, CompoundStorage Storage)>();

        foreach (CompoundStorage child in MsgContainer.GetStorages(parent))
        {
            if (!child.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = child.Name.Substring(prefix.Length);
            if (suffix.Length != 8 || !uint.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint index))
            {
                if (strict)
                {
                    throw new OutlookMsgFormatException(string.Format(
                        CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgStorageIndex, child.Name));
                }

                continue;
            }

            found.Add((index, child));
        }

        // A stable ordering keeps directory order among equal indexes, so the tolerant de-duplication below is
        // deterministic.
        found = [.. found.OrderBy(static entry => entry.Index)];

        if (strict)
        {
            for (int i = 0; i < found.Count; i++)
            {
                if (found[i].Index != (uint)i)
                {
                    throw new OutlookMsgFormatException(string.Format(
                        CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgStorageIndex, found[i].Storage.Name));
                }
            }

            if (found.Count != declaredCount)
            {
                throw new OutlookMsgFormatException(string.Format(
                    CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgStorageCount, prefix, declaredCount, found.Count));
            }
        }

        var storages = new List<CompoundStorage>(found.Count);
        uint? previous = null;
        foreach ((uint index, CompoundStorage storage) in found)
        {
            if (index == previous)
                continue;

            storages.Add(storage);
            previous = index;
        }

        return storages;
    }
}
