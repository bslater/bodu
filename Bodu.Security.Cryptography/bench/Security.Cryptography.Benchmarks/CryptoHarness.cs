// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHarness.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Measures throughput, latency, and allocation for the primitives the cryptography performance plans
/// (<c>plans/crypto-performance.md</c> and <c>plans/crypto-performance-followups.md</c>) target, next to the BCL and - on
/// Linux - OpenSSL on the same machine.
/// </summary>
/// <remarks>
/// <para>
/// Run with <c>--crypto-harness</c>, optionally followed by case filters: a case runs when its <c>group/name</c>
/// contains any filter, ignoring case (for example <c>--crypto-harness SHAKE scrypt</c>).
/// <c>--round &lt;seconds&gt;</c> sets the length of each of the five measured rounds (0.4 s by default).
/// </para>
/// <para>
/// Each case is warmed up until it has run at least 60 times or for half a second, capped at 1.5 s so the slowest cases
/// still finish, which is long enough for tiered compilation to promote the hot methods. The reported time is the
/// median of five rounds; allocation is the process-wide allocation per operation over all five.
/// </para>
/// <para>
/// Built with <c>-p:BoduCryptoBaseline=1.0.0</c>, the same source measures the published package, leaving out the cases
/// for APIs 1.0.0 lacks. SIMD tiers are selected with the runtime's switches: <c>DOTNET_EnableAVX512F=0</c> (.NET 8) or
/// <c>DOTNET_EnableAVX512=0</c> (.NET 10) removes AVX-512, <c>DOTNET_EnableAVX2=0</c> AVX2, and
/// <c>DOTNET_EnableHWIntrinsic=0</c> every vector path, the BCL's included. <c>--disable-simd</c> sets the library's
/// own switch instead, so only the library's kernels give way to their scalar paths.
/// </para>
/// </remarks>
internal static class CryptoHarness
{
    /// <summary>The number of measured rounds per case; the median is reported.</summary>
    private const int Rounds = 5;

    /// <summary>The minimum number of warm-up operations per case.</summary>
    private const int WarmUpOperations = 60;

    /// <summary>The size of the bulk-throughput input.</summary>
    private const int BulkLength = 1 << 20;

    /// <summary>The size of the small-message input.</summary>
    private const int SmallLength = 64;

    /// <summary>The message lengths the Poly1305 cases sweep: one AEAD block, then up to the bulk input.</summary>
    private static readonly int[] s_macLengths = [SmallLength, 256, 1 << 10, 16 << 10, BulkLength];

    /// <summary>
    /// The message lengths below 1 KiB the AEAD cases sweep as well: with those of <see cref="s_macLengths" />, every
    /// number of keystream blocks up to nine, then twelve and fifteen.
    /// </summary>
    private static readonly int[] s_shortAeadLengths = [0, 16, 128, 192, 320, 384, 448, 512, 576, 768, 960];

    /// <summary>The message lengths below 256 bytes the MAC cases add, from 128 bytes, where Apple silicon's AdvSimd kernel takes over.</summary>
    private static readonly int[] s_macThresholdLengths = [128, 192];

    /// <summary>
    /// The message lengths the Poly1305 kernel cases sweep: each side of every length at which dispatch moves from one
    /// kernel to the next, then up to the bulk input.
    /// </summary>
    private static readonly int[] s_kernelLengths = [64, 128, 192, 256, 384, 512, 768, 1 << 10, 2 << 10, 4 << 10, 16 << 10, BulkLength];

    /// <summary>The case filters from the command line; empty to run every case.</summary>
    private static string[] s_filters = [];

    /// <summary>The length of each measured round.</summary>
    private static TimeSpan s_round = TimeSpan.FromSeconds(0.4);

    /// <summary>
    /// Runs the harness.
    /// </summary>
    /// <param name="args">
    /// The arguments after <c>--crypto-harness</c>: case filters, and <c>--round &lt;seconds&gt;</c>.
    /// </param>
    internal static void Run(string[] args)
    {
        var filters = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--round" && i + 1 < args.Length)
                s_round = TimeSpan.FromSeconds(double.Parse(args[++i], CultureInfo.InvariantCulture));
            else
                filters.AddRange(args[i].Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        s_filters = [.. filters];
        Console.WriteLine(Describe());

        RunHashes();
        RunStreamCiphers();
        RunAeadsAndModes();
#if !BODU_CRYPTO_BASELINE
        RunPoly1305Kernels();
#endif
        RunBlockCiphers();
        RunKeyDerivation();
#if !BODU_CRYPTO_BASELINE
        RunArgon2Kernels();
#endif
        RunPublicKey();
    }

    /// <summary>
    /// Measures the hash and XOF cases.
    /// </summary>
    private static void RunHashes()
    {
        byte[] bulk = Random(BulkLength, 1);
        byte[] small = Random(SmallLength, 2);
        byte[] digest = new byte[128];

        foreach ((string name, HashAlgorithm algorithm) in new (string, HashAlgorithm)[]
        {
            ("BLAKE2b-512", new Blake2b()),
            ("BLAKE2s-256", new Blake2s()),
            ("BLAKE3", new Blake3()),
            ("Skein-512", new Skein512()),
            ("CubeHash", new CubeHash()),
            ("SHAKE128", new Shake(256, 128)),
            ("SHAKE256", new Shake(512, 256)),
            ("Whirlpool", new Whirlpool()),
            ("Tiger", new Tiger()),
        })
        {
            Measure("hash", $"Bodu {name} 1 MiB", BulkLength, () => algorithm.TryComputeHash(bulk, digest, out _));
            Measure("hash", $"Bodu {name} 64 B", SmallLength, () => algorithm.TryComputeHash(small, digest, out _));
        }

        // BLAKE3's tree lets one large input use every core; the bound is opt-in, so both sides are measured.
        byte[] large = Random(16 * BulkLength, 11);
        using (var blake3 = new Blake3())
            Measure("hash", "Bodu BLAKE3 16 MiB", large.Length, () => blake3.TryComputeHash(large, digest, out _));

#if !BODU_CRYPTO_BASELINE
        using (var blake3 = new Blake3(maxDegreeOfParallelism: -1))
        {
            Measure("hash", "Bodu BLAKE3 1 MiB, all cores", BulkLength, () => blake3.TryComputeHash(bulk, digest, out _));
            Measure("hash", "Bodu BLAKE3 16 MiB, all cores", large.Length, () => blake3.TryComputeHash(large, digest, out _));
        }
#endif

        Measure("hash", "BCL SHA-256 1 MiB", BulkLength, () => SHA256.HashData(bulk, digest));
        if (Shake128.IsSupported)
        {
            Measure("hash", "BCL SHAKE128 1 MiB", BulkLength, () => Shake128.HashData(bulk, digest.AsSpan(0, 32)));
            Measure("hash", "BCL SHAKE128 64 B", SmallLength, () => Shake128.HashData(small, digest.AsSpan(0, 32)));
        }

        if (OpenSsl.TryFetchDigest("BLAKE2B-512", out nint blake2b))
            Measure("hash", "OpenSSL BLAKE2b-512 1 MiB", BulkLength, () => OpenSsl.Digest(blake2b, bulk, digest));
        if (OpenSsl.TryFetchDigest("BLAKE2S-256", out nint blake2s))
            Measure("hash", "OpenSSL BLAKE2s-256 1 MiB", BulkLength, () => OpenSsl.Digest(blake2s, bulk, digest));
    }

    /// <summary>
    /// Measures keystream generation for each stream cipher, continuing one transform across operations.
    /// </summary>
    private static void RunStreamCiphers()
    {
        byte[] bulk = Random(BulkLength, 3);
        byte[] output = new byte[BulkLength];

        foreach ((string name, SymmetricStreamAlgorithm algorithm) in new (string, SymmetricStreamAlgorithm)[]
        {
            ("ChaCha20", new ChaCha20()),
            ("XChaCha20", new XChaCha20()),
            ("Salsa20", new Salsa20()),
            ("XSalsa20", new XSalsa20()),
            ("HC-128", new Hc128()),
            ("Rabbit", new Rabbit()),
        })
        {
            ICryptoTransform transform = algorithm.CreateEncryptor();
            Measure("stream", $"Bodu {name} 1 MiB", BulkLength, () => transform.TransformBlock(bulk, 0, bulk.Length, output, 0));
        }
    }

    /// <summary>
    /// Measures the AEADs, Poly1305, and the block-cipher modes, one message per transform as the modes require.
    /// </summary>
    private static void RunAeadsAndModes()
    {
        byte[] bulk = Random(BulkLength, 4);
        byte[] small = Random(SmallLength, 5);
        byte[] output = new byte[BulkLength + 64];
        byte[] tag = new byte[16];
        byte[] digest = new byte[16];
        byte[] key16 = Random(16, 6);
        byte[] key32 = Random(32, 7);
        byte[] nonce12 = Random(12, 8);
        byte[] nonce24 = Random(24, 9);
        byte[] iv16 = Random(16, 10);

        // The Poly1305 family over a sweep of lengths, from one AEAD block to the bulk input, to show where each
        // Poly1305 kernel takes over from the one below it.
        using var bcl = ChaCha20Poly1305.IsSupported ? new ChaCha20Poly1305(key32) : null;
        bool openSslMac = OpenSsl.TryCreateMac("POLY1305", out nint openSslPoly1305);
        foreach (int length in s_macLengths)
        {
            byte[] message = length switch { BulkLength => bulk, SmallLength => small, _ => Random(length, 14) };
            string size = SizeLabel(length);
            Measure("aead", $"Bodu XChaCha20-Poly1305 {size}", length, () => { using var aead = new XChaCha20Poly1305(key32, nonce24); aead.Encrypt(message, output); });
            Measure("aead", $"Bodu XSalsa20-Poly1305 {size}", length, () => { using var aead = new XSalsa20Poly1305(key32, nonce24); aead.Encrypt(message, output); });
            if (bcl is not null)
                Measure("aead", $"BCL ChaCha20-Poly1305 {size}", length, () => bcl.Encrypt(nonce12, message, output.AsSpan(0, length), tag));

            Measure("mac", $"Bodu Poly1305 {size}", length, () => { using var mac = new Poly1305(); mac.Key = key32; mac.TryComputeHash(message, digest, out _); });
            if (openSslMac)
                Measure("mac", $"OpenSSL Poly1305 {size}", length, () => OpenSsl.Mac(openSslPoly1305, key32, message, digest));
        }

        // The AEADs again over short messages, whose few keystream blocks, with the block that keys Poly1305 and the
        // derivation of the subkey, make up most of their cost.
        foreach (int length in s_shortAeadLengths)
        {
            byte[] message = Random(length, 15);
            string size = SizeLabel(length);
            Measure("aead", $"Bodu XChaCha20-Poly1305 {size}", length, () => { using var aead = new XChaCha20Poly1305(key32, nonce24); aead.Encrypt(message, output); });
            Measure("aead", $"Bodu XSalsa20-Poly1305 {size}", length, () => { using var aead = new XSalsa20Poly1305(key32, nonce24); aead.Encrypt(message, output); });
            if (bcl is not null)
                Measure("aead", $"BCL ChaCha20-Poly1305 {size}", length, () => bcl.Encrypt(nonce12, message, output.AsSpan(0, length), tag));
        }

        // The MAC alone between the AdvSimd kernel's two thresholds: Apple silicon's kernel takes runs from 128 bytes,
        // every other ARM64 processor's from 256.
        foreach (int length in s_macThresholdLengths)
        {
            byte[] message = Random(length, 16);
            Measure("mac", $"Bodu Poly1305 {SizeLabel(length)}", length, () => { using var mac = new Poly1305(); mac.Key = key32; mac.TryComputeHash(message, digest, out _); });
        }

        using (var aes = new AesBlockCipher(key16))
        using (var aes2 = new AesBlockCipher(key32[..16]))
        {
            Measure("aead", "Bodu AES-128-GCM 1 MiB", BulkLength, () => { using var gcm = new GcmModeTransform(aes, nonce12); gcm.Encrypt(bulk, output); });
            Measure("aead", "Bodu AES-128-GCM 64 B", SmallLength, () => { using var gcm = new GcmModeTransform(aes, nonce12); gcm.Encrypt(small, output); });
            Measure("aead", "Bodu AES-128-GCM-SIV 1 MiB", BulkLength, () => { using var siv = new GcmSivModeTransform(aes, static k => new AesBlockCipher(k), iv16); siv.Encrypt(bulk, output); });
            Measure("aead", "Bodu AES-128-GCM-SIV 64 B", SmallLength, () => { using var siv = new GcmSivModeTransform(aes, static k => new AesBlockCipher(k), iv16); siv.Encrypt(small, output); });
            Measure("aead", "Bodu AES-128-CCM 1 MiB", BulkLength, () => { using var ccm = new CcmModeTransform(aes, iv16); ccm.Encrypt(bulk, output); });
            Measure("aead", "Bodu AES-128-EAX 1 MiB", BulkLength, () => { using var eax = new EaxModeTransform(aes, iv16); eax.Encrypt(bulk, output); });
            Measure("aead", "Bodu AES-128-OCB 1 MiB", BulkLength, () => { using var ocb = new OcbModeTransform(aes, iv16); ocb.Encrypt(bulk, output); });
            Measure("aead", "Bodu AES-128-SIV 1 MiB", BulkLength, () => { using var siv = new SivModeTransform(aes, aes2, iv16); siv.Encrypt(bulk, output); });
            Measure("mode", "Bodu AES-128-CTR 1 MiB", BulkLength, () => { using var ctr = new CtrModeTransform(aes, iv16); ctr.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: true); });
            Measure("mode", "Bodu AES-128-CBC encrypt 1 MiB", BulkLength, () => { using var cbc = new CbcModeTransform(aes, iv16); cbc.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: true); });
            Measure("mode", "Bodu AES-128-CBC decrypt 1 MiB", BulkLength, () => { using var cbc = new CbcModeTransform(aes, iv16); cbc.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: false); });
            Measure("mode", "Bodu AES-128-CFB decrypt 1 MiB", BulkLength, () => { using var cfb = new CfbModeTransform(aes, iv16); cfb.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: false); });
            Measure("mode", "Bodu AES-128-XTS 1 MiB", BulkLength, () => { using var xts = new XtsModeTransform(aes, aes2, iv16); xts.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: true); });
            Measure("mode", "Bodu AES-128-ECB bulk 1 MiB", BulkLength, () => aes.EncryptBlocks(bulk, output));
        }

        using (var bclGcm = new AesGcm(key16, 16))
            Measure("aead", "BCL AES-128-GCM 1 MiB", BulkLength, () => bclGcm.Encrypt(nonce12, bulk, output.AsSpan(0, BulkLength), tag));

        foreach ((string name, Func<IBlockCipher> create, int ivLength) in new (string, Func<IBlockCipher>, int)[]
        {
            ("Serpent-128", () => new Serpent128Cipher(key32), 16),
            ("Twofish", () => new TwofishBlockCipher(key32), 16),
            ("Camellia", () => new CamelliaBlockCipher(key32), 16),
            ("Threefish-512", () => new Threefish512Cipher(Random(64, 11), Random(16, 12)), 64),
        })
        {
            using IBlockCipher cipher = create();
            byte[] iv = Random(ivLength, 13);
            Measure("mode", $"Bodu {name}-CTR 1 MiB", BulkLength, () => { using var ctr = new CtrModeTransform(cipher, iv); ctr.Transform(bulk, output.AsSpan(0, BulkLength), encrypt: true); });
            Measure("mode", $"Bodu {name}-CTR 64 B", SmallLength, () => { using var ctr = new CtrModeTransform(cipher, iv); ctr.Transform(small, output.AsSpan(0, SmallLength), encrypt: true); });

            // The same blocks encrypted without the counter mode: what CTR's keystream costs before the counters and
            // the XOR.
            Measure("mode", $"Bodu {name}-ECB bulk 1 MiB", BulkLength, () => cipher.EncryptBlocks(bulk, output.AsSpan(0, BulkLength)));
        }
    }

#if !BODU_CRYPTO_BASELINE
    /// <summary>
    /// Measures Poly1305 through each kernel the processor supports, named explicitly, at lengths either side of the
    /// dispatch thresholds, to show where each kernel overtakes the one below it.
    /// </summary>
    /// <remarks>
    /// The library's switch does not reach a kernel named explicitly, so with <c>--disable-simd</c> these cases would
    /// repeat the default configuration's, and they are left out.
    /// </remarks>
    private static void RunPoly1305Kernels()
    {
        if (Program.IsSimdDisabled)
            return;

        var driver = Poly1305KernelDriver.Create();
        byte[] key = Random(32, 16);
        byte[] tag = new byte[16];
        foreach (int length in s_kernelLengths)
        {
            byte[] message = Random(length, 17);
            foreach ((string name, int kernel) in driver.Kernels)
                Measure("kernel", $"Poly1305 {name} {SizeLabel(length)}", length, () => driver.ComputeTag(kernel, key, message, tag));
        }
    }

    /// <summary>
    /// Measures Argon2id through each compression kernel the processor supports, named explicitly and on the calling
    /// thread, with the parameters of the key-derivation case.
    /// </summary>
    private static void RunArgon2Kernels()
    {
        if (Program.IsSimdDisabled)
            return;

        var driver = Argon2FillDriver.Create();
        byte[] password = Random(32, 21);
        byte[] salt = Random(16, 22);
        var argon2 = new Argon2Parameters { MemoryKiB = 19 * 1024, Iterations = 2, Parallelism = 1 };
        foreach ((string name, int kernel) in driver.Kernels)
            Measure("kernel", $"Argon2id {name} m=19 MiB t=2 p=1", 0, () => driver.DeriveKeyWithKernel(argon2, password, salt, kernel));
    }
#endif

    /// <summary>
    /// Measures block ciphers one block per call, through <see cref="IBlockCipher.Encrypt" /> and
    /// <see cref="IBlockCipher.Decrypt" />: the wide-block Serpent variants, which have no batched path, and Serpent-128
    /// for scale.
    /// </summary>
    private static void RunBlockCiphers()
    {
        const int Length = 64 << 10;
        byte[] input = Random(Length, 18);
        byte[] output = new byte[Length];
        byte[] tweak = Random(16, 19);

        foreach ((string name, Func<IBlockCipher> create) in new (string, Func<IBlockCipher>)[]
        {
            ("Serpent-128", () => new Serpent128Cipher(Random(32, 20))),
            ("Serpent-256", () => new Serpent256Cipher(Random(32, 20), tweak)),
            ("Serpent-512", () => new Serpent512Cipher(Random(64, 20), tweak)),
            ("Serpent-1024", () => new Serpent1024Cipher(Random(128, 20), tweak)),
        })
        {
            using IBlockCipher cipher = create();
            int blockLength = cipher.BlockSize / 8;
            Measure("block", $"Bodu {name} encrypt, block per call", Length, () =>
            {
                for (int offset = 0; offset < Length; offset += blockLength)
                    cipher.Encrypt(input.AsSpan(offset, blockLength), output.AsSpan(offset, blockLength));
            });
            Measure("block", $"Bodu {name} decrypt, block per call", Length, () =>
            {
                for (int offset = 0; offset < Length; offset += blockLength)
                    cipher.Decrypt(input.AsSpan(offset, blockLength), output.AsSpan(offset, blockLength));
            });
        }
    }

    /// <summary>
    /// Measures scrypt at the interactive and OWASP-minimum costs, next to OpenSSL's scrypt, with Argon2id for scale;
    /// the four-unit cost runs again with its units on four threads.
    /// </summary>
    private static void RunKeyDerivation()
    {
        byte[] password = Random(16, 14);
        byte[] salt = Random(16, 15);
        byte[] key = new byte[32];

        foreach ((int log2N, int r, int p) in new[] { (14, 8, 1), (14, 8, 4), (17, 8, 1) })
        {
            string parameters = $"N=2^{log2N} r={r} p={p}";
            Measure("kdf", $"Bodu scrypt {parameters}", 0, () => Scrypt.DeriveKey(password, salt, 1 << log2N, r, p, key));
            if (OpenSsl.IsAvailable)
                Measure("kdf", $"OpenSSL scrypt {parameters}", 0, () => OpenSsl.Scrypt(password, salt, 1UL << log2N, (ulong)r, (ulong)p, key));
        }

#if !BODU_CRYPTO_BASELINE
        var threaded = new Scrypt(1 << 14, 8, 4, maxDegreeOfParallelism: 4);
        Measure("kdf", "Bodu scrypt N=2^14 r=8 p=4 bound 4", 0, () => threaded.DeriveKey(password, salt, key));
#endif

        var argon2 = new Argon2Parameters { MemoryKiB = 19 * 1024, Iterations = 2, Parallelism = 1 };
        Measure("kdf", "Bodu Argon2id m=19 MiB t=2 p=1", 0, () => Argon2id.DeriveKey(password, salt, argon2));
    }

    /// <summary>
    /// Measures the Curve25519 and post-quantum operations.
    /// </summary>
    private static void RunPublicKey()
    {
        byte[] message = Random(64, 16);

        using (var alice = new X25519())
        using (var bob = new X25519())
        {
            alice.GenerateKey();
            bob.GenerateKey();
            byte[] peer = bob.ExportPublicKey();
            Measure("asym", "Bodu X25519 shared secret", 0, () => alice.DeriveSharedSecret(peer));
            Measure("asym", "Bodu X25519 key generation", 0, alice.GenerateKey);
        }

        using (var ed25519 = new Ed25519())
        {
            ed25519.GenerateKey();
            byte[] signature = ed25519.SignData(message);
            Measure("asym", "Bodu Ed25519 sign", 0, () => ed25519.SignData(message));
            Measure("asym", "Bodu Ed25519 verify", 0, () => ed25519.VerifyData(message, signature));

            // Signing into a span allocates nothing for the signature, so its B/op is the hashing's alone. The longer
            // messages show what the hashing costs as a message grows.
            byte[] destination = new byte[Ed25519.SignatureSizeInBytes];
            Measure("asym", "Bodu Ed25519 sign into a span", 0, () => ed25519.SignData(message, destination));
            foreach (int length in new[] { 1 << 10, 16 << 10 })
            {
                byte[] longer = Random(length, 17);
                byte[] longerSignature = ed25519.SignData(longer);
                string size = SizeLabel(length);
                Measure("asym", $"Bodu Ed25519 sign {size} into a span", 0, () => ed25519.SignData(longer, destination));
                Measure("asym", $"Bodu Ed25519 verify {size}", 0, () => ed25519.VerifyData(longer, longerSignature));
            }
        }

        MeasureKem("512", () => new MLKem512());
        MeasureKem("768", () => new MLKem768());
        MeasureKem("1024", () => new MLKem1024());
        MeasureDsa("44", () => new MLDsa44(), message);
        MeasureDsa("65", () => new MLDsa65(), message);
        MeasureDsa("87", () => new MLDsa87(), message);
    }

    /// <summary>
    /// Measures one ML-KEM parameter set: key generation, encapsulation and decapsulation with the values the key keeps,
    /// and encapsulation to a key imported for that one encapsulation, as a peer's key is.
    /// </summary>
    /// <param name="level">The parameter set's number, such as 768.</param>
    /// <param name="create">Creates an instance of the parameter set.</param>
    private static void MeasureKem(string level, Func<MLKem> create)
    {
        string name = "Bodu ML-KEM-" + level;
        using MLKem kem = create();
        using MLKem recipient = create();

        Measure("pq", name + " key generation", 0, kem.GenerateKey);
        kem.GenerateKey();
        byte[] encapsulationKey = kem.ExportEncapsulationKey();
        (byte[] ciphertext, _) = kem.Encapsulate();
        Measure("pq", name + " encapsulate", 0, () => kem.Encapsulate());
        Measure("pq", name + " decapsulate", 0, () => kem.Decapsulate(ciphertext));
        Measure("pq", name + " encapsulate to an imported key", 0, () =>
        {
            recipient.ImportEncapsulationKey(encapsulationKey);
            recipient.Encapsulate();
        });
    }

    /// <summary>
    /// Measures one ML-DSA parameter set: key generation, signing and verification with the values the key keeps, and
    /// verification with a public key imported for that one verification, as a signer's key is.
    /// </summary>
    /// <param name="level">The parameter set's number, such as 65.</param>
    /// <param name="create">Creates an instance of the parameter set.</param>
    /// <param name="message">The message to sign and verify.</param>
    private static void MeasureDsa(string level, Func<MLDsa> create, byte[] message)
    {
        string name = "Bodu ML-DSA-" + level;
        using MLDsa dsa = create();
        using MLDsa verifier = create();

        Measure("pq", name + " key generation", 0, dsa.GenerateKey);
        dsa.GenerateKey();
        byte[] publicKey = dsa.ExportPublicKey();
        byte[] signature = dsa.SignData(message);
        Measure("pq", name + " sign", 0, () => dsa.SignData(message));
        Measure("pq", name + " verify", 0, () => dsa.VerifyData(message, signature));
        Measure("pq", name + " verify with an imported key", 0, () =>
        {
            verifier.ImportPublicKey(publicKey);
            verifier.VerifyData(message, signature);
        });
    }

    /// <summary>
    /// Warms one case up, measures it over five rounds, and prints its median time, rate, allocation, and gen2 count.
    /// </summary>
    /// <param name="group">The case's group, printed first and matched by the filters.</param>
    /// <param name="name">The case's name.</param>
    /// <param name="bytesPerOperation">
    /// The bytes each operation processes, or 0 to report operations per second.
    /// </param>
    /// <param name="operation">The operation to measure.</param>
    private static void Measure(string group, string name, long bytesPerOperation, Action operation)
    {
        string label = group + "/" + name;
        if (s_filters.Length > 0 && !s_filters.Any(filter => label.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            return;

        var stopwatch = Stopwatch.StartNew();
        int warmUps = 0;
        while ((warmUps < WarmUpOperations || stopwatch.Elapsed.TotalSeconds < 0.5) && stopwatch.Elapsed.TotalSeconds < 1.5)
        {
            operation();
            warmUps++;
        }

        // Tier-1 code is installed on a background thread; give it a moment, then run the promoted code once more.
        Thread.Sleep(150);
        for (int i = 0; i < Math.Min(warmUps, 20); i++)
            operation();

        var perOperation = new double[Rounds];
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int gen2Before = GC.CollectionCount(2);
        long operations = 0;
        for (int round = 0; round < Rounds; round++)
        {
            stopwatch.Restart();
            int count = 0;
            while (stopwatch.Elapsed < s_round || count < 2)
            {
                operation();
                count++;
            }

            perOperation[round] = stopwatch.Elapsed.TotalSeconds / count;
            operations += count;
        }

        long allocated = (GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore) / operations;
        int gen2 = GC.CollectionCount(2) - gen2Before;
        Array.Sort(perOperation);
        double median = perOperation[Rounds / 2];

        string time = median >= 1e-3
            ? string.Create(CultureInfo.InvariantCulture, $"{median * 1e3,9:F2} ms")
            : string.Create(CultureInfo.InvariantCulture, $"{median * 1e6,9:F2} us");
        string rate = bytesPerOperation > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{bytesPerOperation / median / (1 << 20),10:F1} MiB/s")
            : string.Create(CultureInfo.InvariantCulture, $"{1 / median,10:F0} op/s ");
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{group,-7} {name,-40} {time}  {rate}  {allocated,11:N0} B/op  gen2 {gen2}"));
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] Random(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }

    /// <summary>
    /// Formats a length for a case name: in bytes below 1 KiB, in KiB below 1 MiB, and in MiB from there.
    /// </summary>
    /// <param name="length">The length, in bytes.</param>
    /// <returns>The label, such as <c>64 B</c>, <c>16 KiB</c> or <c>1 MiB</c>.</returns>
    private static string SizeLabel(int length) => length switch
    {
        >= 1 << 20 => string.Create(CultureInfo.InvariantCulture, $"{length >> 20} MiB"),
        >= 1 << 10 => string.Create(CultureInfo.InvariantCulture, $"{length >> 10} KiB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{length} B"),
    };

    /// <summary>
    /// Describes the build and the host the numbers come from.
    /// </summary>
    /// <returns>
    /// The package version under test, the runtime, the processor count, and the vector sets enabled.
    /// </returns>
    private static string Describe()
    {
        string version = typeof(Scrypt).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        string isa = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? $"AdvSimd={System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported}"
            : $"AVX-512={System.Runtime.Intrinsics.X86.Avx512F.IsSupported} AVX2={System.Runtime.Intrinsics.X86.Avx2.IsSupported} SSSE3={System.Runtime.Intrinsics.X86.Ssse3.IsSupported}";
        string simd = Program.IsSimdDisabled ? "library SIMD off" : "library SIMD on";
        return $"Bodu.Security.Cryptography {version}; {RuntimeInformation.FrameworkDescription}; {Environment.ProcessorCount} processors; {isa}; {simd}; OpenSSL references {(OpenSsl.IsAvailable ? "on" : "off")}";
    }

    /// <summary>
    /// Calls OpenSSL's <c>libcrypto.so.3</c> for reference measurements on Linux.
    /// </summary>
    private static class OpenSsl
    {
        /// <summary>The name of the library the references call.</summary>
        private const string Library = "libcrypto.so.3";

        /// <summary>
        /// Gets a value indicating whether <c>libcrypto.so.3</c> loads in this process.
        /// </summary>
        internal static bool IsAvailable { get; } = OperatingSystem.IsLinux() && NativeLibrary.TryLoad(Library, out _);

        /// <summary>
        /// Fetches a digest implementation by name.
        /// </summary>
        /// <param name="name">The OpenSSL algorithm name.</param>
        /// <param name="digest">The fetched <c>EVP_MD</c>, or zero.</param>
        /// <returns><see langword="true" /> if OpenSSL is available and has the algorithm.</returns>
        internal static bool TryFetchDigest(string name, out nint digest)
        {
            digest = IsAvailable ? EVP_MD_fetch(0, name, 0) : 0;
            return digest != 0;
        }

        /// <summary>
        /// Hashes <paramref name="data" /> into <paramref name="digest" /> with a fetched digest.
        /// </summary>
        /// <param name="md">The fetched <c>EVP_MD</c>.</param>
        /// <param name="data">The input.</param>
        /// <param name="digest">The destination, at least the digest's size.</param>
        internal static void Digest(nint md, byte[] data, byte[] digest)
        {
            if (EVP_Digest(data, (nuint)data.Length, digest, out _, md, 0) != 1) throw new CryptographicException("EVP_Digest failed.");
        }

        /// <summary>
        /// Fetches a MAC implementation by name and creates a context for it.
        /// </summary>
        /// <param name="name">The OpenSSL algorithm name.</param>
        /// <param name="context">The created <c>EVP_MAC_CTX</c>, or zero.</param>
        /// <returns><see langword="true" /> if OpenSSL is available and has the algorithm.</returns>
        internal static bool TryCreateMac(string name, out nint context)
        {
            nint mac = IsAvailable ? EVP_MAC_fetch(0, name, 0) : 0;
            context = mac != 0 ? EVP_MAC_CTX_new(mac) : 0;
            return context != 0;
        }

        /// <summary>
        /// Keys a MAC context afresh, as a one-time key requires, and computes the MAC of <paramref name="data" />.
        /// </summary>
        /// <param name="context">The <c>EVP_MAC_CTX</c>.</param>
        /// <param name="key">The key.</param>
        /// <param name="data">The input.</param>
        /// <param name="mac">The destination, at least the MAC's size.</param>
        internal static void Mac(nint context, byte[] key, byte[] data, byte[] mac)
        {
            if (EVP_MAC_init(context, key, (nuint)key.Length, 0) != 1 || EVP_MAC_update(context, data, (nuint)data.Length) != 1 || EVP_MAC_final(context, mac, out _, (nuint)mac.Length) != 1)
                throw new CryptographicException("EVP_MAC failed.");
        }

        /// <summary>
        /// Derives a scrypt key with OpenSSL.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <param name="salt">The salt.</param>
        /// <param name="n">The CPU/memory cost.</param>
        /// <param name="r">The block size.</param>
        /// <param name="p">The parallelization.</param>
        /// <param name="key">The destination key.</param>
        internal static void Scrypt(byte[] password, byte[] salt, ulong n, ulong r, ulong p, byte[] key)
        {
            if (EVP_PBE_scrypt(password, (nuint)password.Length, salt, (nuint)salt.Length, n, r, p, 2UL << 30, key, (nuint)key.Length) != 1)
                throw new CryptographicException("EVP_PBE_scrypt failed.");
        }

        [DllImport(Library)]
        private static extern nint EVP_MD_fetch(nint context, string algorithm, nint properties);

        [DllImport(Library)]
        private static extern int EVP_Digest(byte[] data, nuint count, byte[] md, out uint size, nint type, nint engine);

        [DllImport(Library)]
        private static extern nint EVP_MAC_fetch(nint context, string algorithm, nint properties);

        [DllImport(Library)]
        private static extern nint EVP_MAC_CTX_new(nint mac);

        [DllImport(Library)]
        private static extern int EVP_MAC_init(nint context, byte[] key, nuint keyLength, nint parameters);

        [DllImport(Library)]
        private static extern int EVP_MAC_update(nint context, byte[] data, nuint length);

        [DllImport(Library)]
        private static extern int EVP_MAC_final(nint context, byte[] output, out nuint outputLength, nuint outputSize);

        [DllImport(Library)]
        private static extern int EVP_PBE_scrypt(byte[] pass, nuint passLength, byte[] salt, nuint saltLength, ulong n, ulong r, ulong p, ulong maxMemory, byte[] key, nuint keyLength);
    }
}
