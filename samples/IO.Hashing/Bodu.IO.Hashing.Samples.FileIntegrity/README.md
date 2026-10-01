# Bodu.IO.Hashing.Samples.FileIntegrity

The `Bodu.IO.Hashing.Extensions` surface applied to real files: `NonCryptographicHashAlgorithmExtensions` adds the
`Stream`, `async`, and verify members that `System.IO.Hashing.NonCryptographicHashAlgorithm` does not have. Three
scenarios covering building a checksum manifest for a release folder (with the same extensions over the framework's
own `Crc32` and `XxHash64`), checking a damaged folder against a damaged manifest, and digesting a file delivered as
numbered parts without reassembling it.

Everything runs offline and deterministically. The release files are written to a temporary folder with fixed
contents and deleted when the sample ends; the folder's path is never printed, so every line of output is
reproducible.

```bash
dotnet run --project samples/IO.Hashing/Bodu.IO.Hashing.Samples.FileIntegrity
```

For NuGet consumers:

```bash
dotnet add package Bodu.IO.Hashing
```

> **Not security.** A CRC detects *accidental* corruption - a bad disk, a truncated copy, a flipped bit in transit.
> Anyone who can change a file can change its manifest entry to match. When the threat is deliberate tampering, use
> a cryptographic digest; `Bodu.Security.Cryptography.Samples.StreamingPipelines` verifies a download that way.

Every scenario opens by printing a **What / Why / Expect** banner - the same three things this README records per
scenario - so a transcript stands on its own and a reader can tell a correct run from a broken one without opening
the source. The `text` blocks below show the value lines only; run the sample to see the banner above each of them.

## Scenario 1 - BuildManifest

**Intent.** Show that a file can be checksummed straight from disk. `NonCryptographicHashAlgorithm` hashes spans
only, so without the extensions every file would first be read into memory. Show too that the extensions are written
against the `System.IO.Hashing` base class, so the framework's own hashes get them as well.

**What it does.** Computes the CRC-32 (ISO-HDLC) of each of the three release files from a `FileStream`, once with
`ComputeHash(Stream)` and once with `ComputeHashAsync(Stream)` over a stream opened for asynchronous I/O, and records
each digest beside its file name as the manifest. Then hashes `assets.pak` through the same `ComputeHash(Stream)`
extension with `System.IO.Hashing.Crc32` and `XxHash64`.

**What to expect.** One manifest line per file with the two routes agreeing, and the framework's `Crc32` producing
exactly the manifest entry Bodu's `Crc` wrote:

```text
  2D257959  readme.txt      (  2151 B, sync == async: True)
  83AE9C47  app.dat         ( 98304 B, sync == async: True)
  3F087BB5  assets.pak      (262144 B, sync == async: True)

  System.IO.Hashing.Crc32 of assets.pak    : 3F087BB5 (== manifest entry: True)
  System.IO.Hashing.XxHash64 of assets.pak : 052DFE7AA97CA530
```

The digests are the bytes `GetCurrentHash` returns, printed in that order. For a CRC that is little-endian, the low
byte first, which is also how `System.IO.Hashing.Crc32` serializes, so the two agree byte for byte. (The CRC-32 check
value `0xCBF43926` therefore prints as `2639F4CB`; the digest byte-order table in the IO.Hashing streaming guide
explains the convention for every family.) `ComputeHash(Stream)` resets the algorithm first and reads the stream from
its current position to its end, through a pooled 4 KiB buffer; it does not rewind or dispose the stream.

**APIs demonstrated.** `NonCryptographicHashAlgorithmExtensions.ComputeHash(Stream, int)`,
`.ComputeHashAsync(Stream, int, CancellationToken)`; `Crc`, `CrcStandard.CRC32_ISOHDLC`;
`System.IO.Hashing.Crc32`, `System.IO.Hashing.XxHash64`.

## Scenario 2 - VerifyManifest

**Intent.** Build the checker a release pipeline actually needs. It has two different ways to fail, and they need
different responses: the file does not match (re-download it), or the entry could not be checked at all (fix the
manifest or the folder). `VerifyHash` folds both into `false`; the `out bool` overload of `TryVerifyHash` keeps them
apart without exceptions.

**What it does.** Flips one bit in `app.dat`, then appends two damaged lines to the manifest: an entry for
`license.txt`, which was never shipped, and an entry whose digest is not hexadecimal. Checks every entry with
`TryVerifyHash(byte[], byte[], out bool)`, passing `null` for a file that does not exist and for a digest that does
not decode. Then re-checks the binary files straight from their streams with `VerifyHash(Stream, string)` and
`VerifyHashAsync(Stream, string)`.

**What to expect.** The out-bool overload returns `false` only when it could not compare - a `null` input or
expected digest, or an exception - and otherwise reports the comparison in its `out` argument, which is what lets one
`if` produce four statuses. The stream checks agree with the byte-array check:

```text
  OK       readme.txt   (manifest: 2D257959)
  BAD      app.dat      (manifest: 83AE9C47)
  OK       assets.pak   (manifest: 3F087BB5)
  MISSING  license.txt  (manifest: 0BADC0DE)
  INVALID  readme.txt   (manifest: not-a-crc)

  VerifyHash(Stream, string), app.dat         : False
  VerifyHashAsync(Stream, string), app.dat    : False
  VerifyHashAsync(Stream, string), assets.pak : True
```

One flipped bit in 96 KiB is exactly the error a CRC-32 is guaranteed to detect. The stream forms read the file
through a pooled buffer, so a multi-gigabyte file is checked without being loaded; they decode the expected hex
case-insensitively and treat malformed hex as a non-match.

**APIs demonstrated.** `NonCryptographicHashAlgorithmExtensions.TryVerifyHash(byte[], byte[], out bool)`,
`.VerifyHash(Stream, string)`, `.VerifyHashAsync(Stream, string, CancellationToken)`.

## Scenario 3 - MultiPartDigests

**Intent.** Verify an artefact that arrives as several files - a split archive, a chunked upload, rotated logs -
against the whole artefact's published digest, without writing a reassembled copy.

**What it does.** Splits the 256 KiB `assets.pak` into `assets.pak.001` to `.003` (100 KiB, 100 KiB, 56 KiB). Feeds
the parts in order into one `Crc` instance with `AppendData(Stream)` and reads the digest once at the end. Repeats
with `AppendDataAsync(Stream)`, printing `GetCurrentHash` after each part. Finally feeds the parts out of order.

**What to expect.** Both in-order digests equal the whole-file digest. The running value changes after every part
and reaches the whole-file value after the last one; `GetCurrentHash` reads it without finalizing or resetting.
Out of order, the digest differs, because a CRC depends on byte order, so a checker that finds the parts by wildcard
must sort them first:

```text
  assets.pak, whole file   : 3F087BB5
  AppendData, in order     : 3F087BB5 (== whole: True)
    after assets.pak.001   : 35A0B34A (102400 B)
    after assets.pak.002   : 530F7BEE (102400 B)
    after assets.pak.003   : 3F087BB5 ( 57344 B)
  AppendDataAsync, in order: 3F087BB5 (== whole: True)
  AppendData, out of order : 77241672 (== whole: False)
```

Unlike `ComputeHash(Stream)`, `AppendData` does not reset first and does not finalize, which is what lets several
sources accumulate into one digest. `GetHashAndReset` ends each run, so the same instance serves the next one.

**APIs demonstrated.** `NonCryptographicHashAlgorithmExtensions.AppendData(Stream, int)`,
`.AppendDataAsync(Stream, int, CancellationToken)`, `.ComputeHash(Stream, int)`;
`NonCryptographicHashAlgorithm.GetCurrentHash`, `.GetHashAndReset`.

## Layout

```text
Bodu.IO.Hashing.Samples.FileIntegrity/
  Program.cs                          # creates the release folder and runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect banner every scenario prints through
  ReleaseFolder.cs                    # the temporary folder of fixed release files, deleted on dispose
  Hex.cs                              # decodes a manifest digest, or reports it malformed
  Scenarios/BuildManifest.cs
  Scenarios/VerifyManifest.cs
  Scenarios/MultiPartDigests.cs
```

## Related

- `Bodu.IO.Hashing.Samples.ChecksumTour` - the CRC catalogue and the checksum families, `HashingStream` for
  checksumming while copying, and `IResumableHashAlgorithm` for extending a stored digest.
- `Bodu.Security.Cryptography.Samples.StreamingPipelines` - the same stream, async, and verify extension shapes for
  the cryptographic hashes and ciphers, where the threat is deliberate tampering.
