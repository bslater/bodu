// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.CipherModesAndPadding.Scenarios;

/// <summary>
/// Demonstrates the <see cref="IPaddingStrategy" /> implementations as standalone objects: what each scheme writes,
/// which ones can recover the original length on unpad, and how <see cref="PaddingFactory" /> selects one.
/// </summary>
public static class PaddingStrategies
{
    /// <summary>
    /// The block size every example pads to, <em>in bits</em>. 64 bits is 8 bytes.
    /// </summary>
    /// <remarks>
    /// <see cref="IPaddingStrategy.Pad" /> takes its block size in <b>bits</b>, not bytes, matching the BCL's
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.BlockSize" /> convention. Passing 8 here would ask
    /// for alignment to a one-byte boundary, which every scheme satisfies trivially — an easy and silent mistake, so
    /// the constant is named for its unit.
    /// </remarks>
    private const int BlockSizeBits = 64;

    /// <summary>The same block size in bytes, for display and for building inputs.</summary>
    private const int BlockSizeBytes = BlockSizeBits / 8;

    /// <summary>
    /// Pads and unpads a misaligned and an aligned message under every strategy, then shows the factory.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Padding strategies",
            what: "Pads a 5-byte and an already-aligned 8-byte message with each of the seven IPaddingStrategy implementations at an 8-byte block, prints the trailer bytes each one actually writes, unpads them, then shows the two rejections and what PaddingFactory maps every PaddingMode onto.",
            why: "A block cipher only consumes whole blocks, so a message has to be extended - and the scheme decides whether the original length survives the trip. That is what the StripsPaddingOnUnpad flag names: PKCS#7, ANSI X9.23, ISO 7816-4 and ISO 10126 all record the count or a marker, while zero padding records nothing, so a 5-byte and an 8-byte message can pad to the same bytes.",
            expect: "The aligned 8-byte case still gains a whole extra block under every counted scheme - padding is never skipped, or unpadding could not tell data from filler. ISO 10126's fill is random, so it prints as ?? and changes on every run while every other trailer is fixed. Zeros prints no recovery line because it cannot recover the length; NoPadding rejects a misaligned input with ArgumentException, and a corrupted PKCS#7 count is rejected with CryptographicException.");

        // A block-oriented mode can only transform whole blocks, so a message of arbitrary length has to be extended
        // first. Padding is deliberately *not* part of IBlockCipherModeTransform: the two are composed by the caller,
        // which is what lets a mode that needs no padding skip it entirely.
        var misaligned = Hex.Fill(5, 0xAA);   // 5 bytes - three short of a block
        var aligned = Hex.Fill(8, 0xBB);      // exactly one block

        Console.WriteLine($"  block size    : {BlockSizeBits} bits = {BlockSizeBytes} bytes");
        Console.WriteLine($"  misaligned in : {Hex.ToHex(misaligned)} ({misaligned.Length} bytes)");
        Console.WriteLine($"  aligned in    : {Hex.ToHex(aligned)} ({aligned.Length} bytes)");
        Console.WriteLine();

        foreach (var (name, strategy, deterministic) in Strategies())
        {
            Console.WriteLine($"  {name}  (StripsPaddingOnUnpad={strategy.StripsPaddingOnUnpad})");

            // The misaligned case: every scheme extends to the next boundary.
            Report("    5 bytes ", strategy, misaligned, deterministic);

            // The aligned case is where the schemes diverge most visibly. A scheme that must be unambiguously
            // reversible has to add a *whole extra block* when the input is already aligned - otherwise the last byte
            // of real data could be mistaken for a padding count.
            Report("    8 bytes ", strategy, aligned, deterministic);

            Console.WriteLine();
        }

        // ZeroPadding and NoPadding report StripsPaddingOnUnpad=false, and that is a real limitation rather than an
        // implementation gap: neither records how much was added, so Unpad returns the input untouched and the
        // original length is simply lost. They are safe only when the length is known out of band, or when trailing
        // zero bytes are meaningless to the application.
        var zero = new ZeroPadding();
        var zeroPadded = zero.Pad(misaligned, BlockSizeBits);
        Console.WriteLine($"  ZeroPadding round-trip: in {misaligned.Length}B -> padded {zeroPadded.Length}B -> unpadded {zero.Unpad(zeroPadded, BlockSizeBits).Length}B (length not recovered)");

        // NoPadding does not extend at all, so it rejects a misaligned input instead of silently corrupting it.
        var none = new NoPadding();
        Console.WriteLine($"  NoPadding on 5 bytes  : {Throws(() => none.Pad(misaligned, BlockSizeBits))}");
        Console.WriteLine($"  NoPadding on 8 bytes  : {Hex.ToHex(none.Pad(aligned, BlockSizeBits))} (passes through unchanged)");

        // A corrupted padding byte is detected by the schemes that encode a count - which is exactly why padding
        // errors must never be reported distinguishably to a remote caller (the padding-oracle attack).
        var pkcs7 = new Pkcs7Padding();
        var corrupted = pkcs7.Pad(misaligned, BlockSizeBits);
        corrupted[^1] = 0x7F;
        Console.WriteLine($"  Pkcs7 bad count       : {Throws(() => pkcs7.Unpad(corrupted, BlockSizeBits))}");

        // PaddingFactory maps either enum onto a strategy: the BCL's PaddingMode for the five schemes it names, and
        // the library's PaddingModeKind, which adds ISO7816_4.
        Console.WriteLine();
        Console.WriteLine("  PaddingFactory.Create:");
        foreach (var mode in new[] { PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None })
            Console.WriteLine($"    PaddingMode.{mode,-9} -> {PaddingFactory.Create(mode).GetType().Name}");

        Console.WriteLine($"    PaddingModeKind.{PaddingModeKind.ISO7816_4} -> {PaddingFactory.Create(PaddingModeKind.ISO7816_4).GetType().Name} (no BCL equivalent)");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns each strategy with a display name and whether its output is deterministic.
    /// </summary>
    /// <returns>The strategies to exercise.</returns>
    private static IEnumerable<(string Name, IPaddingStrategy Strategy, bool Deterministic)> Strategies()
    {
        yield return ("PKCS#7    ", new Pkcs7Padding(), true);
        yield return ("ANSI X9.23", new Ansix923Padding(), true);
        yield return ("ISO 7816-4", new Iso7816_4Padding(), true);
        yield return ("ISO 10126 ", new Iso10126Padding(), false);
        yield return ("Zeros     ", new ZeroPadding(), true);
    }

    /// <summary>
    /// Pads an input, prints the result, and confirms the round trip.
    /// </summary>
    /// <param name="label">The row label.</param>
    /// <param name="strategy">The strategy to apply.</param>
    /// <param name="input">The input to pad.</param>
    /// <param name="deterministic">Whether the padded bytes are reproducible across runs.</param>
    private static void Report(string label, IPaddingStrategy strategy, byte[] input, bool deterministic)
    {
        var padded = strategy.Pad(input, BlockSizeBits);
        var unpadded = strategy.Unpad(padded, BlockSizeBits);
        var recovered = strategy.StripsPaddingOnUnpad && unpadded.SequenceEqual(input);

        // ISO 10126 fills with random bytes and encodes only the count in the last byte, so its padded form differs
        // on every run. Printing it would document output that cannot be reproduced, so the shape is shown instead.
        var rendered = deterministic
            ? Hex.ToBlocks(padded, BlockSizeBytes)
            : $"{new string('?', (padded.Length - 1) * 2)}{Hex.ToHex(padded.AsSpan(padded.Length - 1))} (random fill, count in the last byte)";

        Console.WriteLine($"{label}-> {padded.Length}B  {rendered}");
        if (strategy.StripsPaddingOnUnpad)
            Console.WriteLine($"{new string(' ', label.Length)}   unpad recovers the original {input.Length} bytes exactly: {recovered}");
    }

    /// <summary>
    /// Invokes an operation expected to fail and names the exception type it raised.
    /// </summary>
    /// <param name="action">The operation to invoke.</param>
    /// <returns>The exception's type name, or a marker when the operation unexpectedly succeeded.</returns>
    private static string Throws(Func<byte[]> action)
    {
        try
        {
            _ = action();
            return "(did not throw)";
        }
        catch (Exception ex)
        {
            return $"rejected ({ex.GetType().Name})";
        }
    }
}
